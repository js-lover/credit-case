using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using FluentValidation;

namespace CreditCase.Application.Services;

/// <summary>
/// Ödeme iş mantığını yönetir: idempotency koruması, taksit durumu güncelleme
/// ve kalan anapara yeniden hesaplama.
/// </summary>
public class PaymentService : IPaymentService
{
    private const int OnTimeBonus   =  5;   // Zamanında ödeme
    private const int LatepenalTy   = -10;  // Gecikmiş ödeme
    private const int MaxBonus      =  200;
    private const int MinBonus      = -200;

    private readonly IPaymentRepository _paymentRepository;
    private readonly IInstallmentRepository _installmentRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<CreatePaymentRequest> _createValidator;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInstallmentRepository installmentRepository,
        ILoanRepository loanRepository,
        ICustomerRepository customerRepository,
        IValidator<CreatePaymentRequest> createValidator)
    {
        _paymentRepository = paymentRepository;
        _installmentRepository = installmentRepository;
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _createValidator = createValidator;
    }

    public async Task<IEnumerable<PaymentResponse>> GetAllAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();
        return payments.Select(MapToResponse);
    }

    /// <summary>
    /// Ödeme oluşturur ve ilgili taksit ile krediyi günceller.
    /// </summary>
    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var installment = await _installmentRepository.GetByIdAsync(request.InstallmentId);
        if (installment is null)
            throw new NotFoundException($"{request.InstallmentId} numaralı taksit bulunamadı.");

        // Katman 1 — Taksit durum kontrolü: entity state üzerinden hızlı kontrol.
        if (installment.Status == InstallmentStatus.Paid)
            throw new BusinessRuleException("Bu taksit zaten ödenmiştir.");

        // Katman 2 — Veritabanı kayıt kontrolü: taksit durumu henüz güncellenmeden servis
        // çökmüşse oluşabilecek çift ödemeyi engeller (taksit Unpaid ama ödeme kaydı var).
        var existingPayment = await _paymentRepository.GetByInstallmentIdAsync(request.InstallmentId);
        if (existingPayment is not null)
            throw new BusinessRuleException("Bu taksit için zaten bir ödeme kaydı mevcut.");

        // Katman 3 — Sıralı ödeme kuralı: önceki taksitler ödenmeden ileri taksit ödenemez.
        // Aynı sorgu ödeme sonrası RemainingPrincipal güncellemesi için de kullanılır.
        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(installment.LoanId);
        if (loan is not null)
        {
            var hasEarlierUnpaid = loan.Installments.Any(i =>
                i.InstallmentNumber < installment.InstallmentNumber &&
                i.Status != InstallmentStatus.Paid);

            if (hasEarlierUnpaid)
                throw new BusinessRuleException("Önceki ödenmemiş taksitler önce ödenmelidir.");
        }

        var payment = new Payment
        {
            InstallmentId = request.InstallmentId,
            PaymentAmount = request.PaymentAmount,
            PaymentDate = DateTime.UtcNow,
            Status = PaymentStatus.Successful
        };

        var created = await _paymentRepository.AddAsync(payment);

        installment.Status = InstallmentStatus.Paid;
        await _installmentRepository.UpdateAsync(installment);

        Customer? customer = null;
        if (loan is not null)
        {
            // Kalan yükümlülük = ödenmemiş taksit tutarlarının toplamı.
            // Güncellenen taksit zaten Paid olduğu için Sum hesabından düşecek.
            loan.RemainingPrincipal = loan.Installments
                .Where(i => i.Id != installment.Id && i.Status != InstallmentStatus.Paid)
                .Sum(i => i.Amount);

            // Tüm taksitler ödenince kredi kapanır.
            if (loan.RemainingPrincipal == 0)
                loan.Status = LoanStatus.Closed;

            await _loanRepository.UpdateAsync(loan);

            // Ödeme davranışına göre müşterinin kredi skoru bonusunu güncelle.
            // Sadece tarih karşılaştırması yapılır; saat farkı görmezden gelinir.
            bool isOnTime = installment.DueDate.Date >= DateTime.UtcNow.Date;
            customer = await _customerRepository.GetByIdAsync(loan.CustomerId);
            if (customer is not null)
            {
                int delta = isOnTime ? OnTimeBonus : LatepenalTy;
                customer.CreditScoreBonus = Math.Clamp(customer.CreditScoreBonus + delta, MinBonus, MaxBonus);
                await _customerRepository.UpdateAsync(customer);
            }
        }

        // Navigation yüklü olmayan created nesnesi yerine hafızadaki verileri kullan.
        return new PaymentResponse
        {
            Id = created.Id,
            InstallmentId = created.InstallmentId,
            InstallmentNumber = installment.InstallmentNumber,
            DueDate = installment.DueDate,
            LoanId = installment.LoanId,
            LoanType = loan?.LoanType ?? default,
            CustomerId = loan?.CustomerId ?? 0,
            CustomerFullName = customer is not null
                ? $"{customer.FirstName} {customer.LastName}"
                : string.Empty,
            PaymentAmount = created.PaymentAmount,
            PaymentDate = created.PaymentDate,
            Status = created.Status
        };
    }

    // GetAllAsync için: navigation chain Include ile yüklü gelir.
    private static PaymentResponse MapToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        InstallmentId = payment.InstallmentId,
        InstallmentNumber = payment.Installment?.InstallmentNumber ?? 0,
        DueDate = payment.Installment?.DueDate ?? default,
        LoanId = payment.Installment?.LoanId ?? 0,
        LoanType = payment.Installment?.Loan?.LoanType ?? default,
        CustomerId = payment.Installment?.Loan?.CustomerId ?? 0,
        CustomerFullName = payment.Installment?.Loan?.Customer is { } c
            ? $"{c.FirstName} {c.LastName}"
            : string.Empty,
        PaymentAmount = payment.PaymentAmount,
        PaymentDate = payment.PaymentDate,
        Status = payment.Status
    };

    // GetAllAsync için: EF Core global query filter ile silinmiş müşteriler Customer null döner.
    // Bu durumda CustomerFullName empty string olur; frontend fallback olarak ID'yi gösterir.
}
