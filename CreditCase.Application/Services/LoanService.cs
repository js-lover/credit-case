using CreditCase.Application.DTOs.Installments;
using CreditCase.Application.DTOs.Loans;
using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using FluentValidation;

namespace CreditCase.Application.Services;

/// <summary>
/// Kredi oluşturma ve sorgulama iş mantığını yönetir.
/// Kredi kaydedilirken taksit planı otomatik olarak üretilir; ayrı bir çağrı gerekmez.
/// </summary>
public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICreditScoreService _creditScoreService;
    private readonly IValidator<CreateLoanRequest> _createValidator;

    public LoanService(
        ILoanRepository loanRepository,
        ICustomerRepository customerRepository,
        ICreditScoreService creditScoreService,
        IValidator<CreateLoanRequest> createValidator)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _creditScoreService = creditScoreService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Tüm kredileri döner. Performans nedeniyle taksitler bu sorguda yüklenmez;
    /// taksit detayları için <see cref="GetByIdAsync"/> kullanılmalıdır.
    /// </summary>
    public async Task<IEnumerable<LoanResponse>> GetAllAsync()
    {
        var loans = await _loanRepository.GetAllAsync();
        return loans.Select(MapToResponse);
    }

    /// <summary>
    /// Krediyi taksit planıyla birlikte döner.
    /// </summary>
    public async Task<LoanResponse> GetByIdAsync(int id)
    {
        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(id);
        if (loan is null)
            throw new NotFoundException($"Loan with ID {id} not found.");
        return MapToResponse(loan);
    }

    /// <summary>
    /// Yeni kredi oluşturur ve taksit planını otomatik üretir.
    /// Oluşturma öncesinde mock kredi skoru servisi sorgulanır; "Approved" dışındaki
    /// sonuçlar 422 hatası olarak reddedilir.
    /// </summary>
    public async Task<LoanResponse> CreateAsync(CreateLoanRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer is null)
            throw new NotFoundException($"Customer with ID {request.CustomerId} not found.");

        // Üçüncü parti kredi skoru kontrolü: gerçek entegrasyonda yalnızca
        // MockCreditScoreService değiştirilir, bu kod değişmez.
        var creditScore = await _creditScoreService.GetCreditScoreAsync(request.CustomerId);
        if (creditScore.Status != "Approved")
            throw new BusinessRuleException($"Loan application rejected. Credit score status: {creditScore.Status}");

        var loan = new Loan
        {
            CustomerId = request.CustomerId,
            LoanType = request.LoanType,
            PrincipalAmount = request.PrincipalAmount,
            InterestRate = request.InterestRate,
            Term = request.Term,
            StartDate = request.StartDate,
            Status = LoanStatus.Active,
            // Başlangıçta kalan anapara = anapara (henüz ödeme yapılmadı).
            RemainingPrincipal = request.PrincipalAmount,
            Installments = GenerateInstallments(request)
        };

        var created = await _loanRepository.AddAsync(loan);
        return MapToResponse(created);
    }

    /// <summary>
    /// Düz faiz (flat-rate) yöntemiyle taksit planı üretir.
    /// <para>
    /// Formül: totalAmount = anapara × (1 + yıllık_faiz/100 × vade_yıl)<br/>
    /// monthlyAmount = ROUND(totalAmount / vade_ay, 2)
    /// </para>
    /// Bu modelde faiz vade boyunca değişmez; her taksit eşit tutardadır.
    /// </summary>
    private static List<Installment> GenerateInstallments(CreateLoanRequest request)
    {
        decimal termYears = request.Term / 12m;
        decimal totalAmount = request.PrincipalAmount * (1 + request.InterestRate / 100 * termYears);
        decimal monthlyAmount = Math.Round(totalAmount / request.Term, 2);

        return Enumerable.Range(1, request.Term)
            .Select(i => new Installment
            {
                InstallmentNumber = i,
                Amount = monthlyAmount,
                // Vade tarihi: kredi başlangıcından i ay sonrası.
                DueDate = request.StartDate.AddMonths(i),
                Status = InstallmentStatus.Unpaid
            })
            .ToList();
    }

    private static LoanResponse MapToResponse(Loan loan) => new()
    {
        Id = loan.Id,
        CustomerId = loan.CustomerId,
        LoanType = loan.LoanType,
        PrincipalAmount = loan.PrincipalAmount,
        InterestRate = loan.InterestRate,
        Term = loan.Term,
        StartDate = loan.StartDate,
        Status = loan.Status,
        RemainingPrincipal = loan.RemainingPrincipal,
        Installments = loan.Installments.Select(i => new InstallmentResponse
        {
            Id = i.Id,
            LoanId = i.LoanId,
            InstallmentNumber = i.InstallmentNumber,
            Amount = i.Amount,
            DueDate = i.DueDate,
            Status = i.Status,
            Payment = i.Payment is null ? null : new PaymentResponse
            {
                Id = i.Payment.Id,
                InstallmentId = i.Payment.InstallmentId,
                PaymentAmount = i.Payment.PaymentAmount,
                PaymentDate = i.Payment.PaymentDate,
                Status = i.Payment.Status
            }
        }).ToList()
    };
}
