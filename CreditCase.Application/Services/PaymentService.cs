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
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInstallmentRepository _installmentRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IValidator<CreatePaymentRequest> _createValidator;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInstallmentRepository installmentRepository,
        ILoanRepository loanRepository,
        IValidator<CreatePaymentRequest> createValidator)
    {
        _paymentRepository = paymentRepository;
        _installmentRepository = installmentRepository;
        _loanRepository = loanRepository;
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
            throw new NotFoundException($"Installment with ID {request.InstallmentId} not found.");

        // Katman 1 — Taksit durum kontrolü: entity state üzerinden hızlı kontrol.
        if (installment.Status == InstallmentStatus.Paid)
            throw new BusinessRuleException("This installment has already been paid.");

        // Katman 2 — Veritabanı kayıt kontrolü: taksit durumu henüz güncellenmeden servis
        // çökmüşse oluşabilecek çift ödemeyi engeller (taksit Unpaid ama ödeme kaydı var).
        var existingPayment = await _paymentRepository.GetByInstallmentIdAsync(request.InstallmentId);
        if (existingPayment is not null)
            throw new BusinessRuleException("A payment already exists for this installment.");

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

        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(installment.LoanId);
        if (loan is not null)
        {
            // Kalan yükümlülük = ödenmemiş taksit tutarlarının toplamı.
            // Önceki formül (anapara / vade × kalan taksit) faizi dışarıda bırakıyordu;
            // bu yaklaşım gerçek kalan ödeme yükümlülüğünü yansıtır.
            loan.RemainingPrincipal = loan.Installments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .Sum(i => i.Amount);

            // Tüm taksitler ödenince kredi kapanır.
            if (loan.RemainingPrincipal == 0)
                loan.Status = LoanStatus.Closed;

            await _loanRepository.UpdateAsync(loan);
        }

        return MapToResponse(created);
    }

    private static PaymentResponse MapToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        InstallmentId = payment.InstallmentId,
        PaymentAmount = payment.PaymentAmount,
        PaymentDate = payment.PaymentDate,
        Status = payment.Status
    };
}
