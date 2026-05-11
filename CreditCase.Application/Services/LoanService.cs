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
    private const int BalloonMinCreditScore = 750;
    private const decimal MinLoanAmount = 1_000m;

    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICreditScoreService _creditScoreService;
    private readonly IRiskAnalysisService _riskAnalysis;
    private readonly IInterestCalculationService _interestCalculation;
    private readonly IMaximumLoanCalculatorService _maxLoanCalculator;
    private readonly IEnumerable<IInstallmentPlanStrategy> _strategies;
    private readonly IValidator<CreateLoanRequest> _createValidator;

    public LoanService(
        ILoanRepository loanRepository,
        ICustomerRepository customerRepository,
        ICreditScoreService creditScoreService,
        IRiskAnalysisService riskAnalysis,
        IInterestCalculationService interestCalculation,
        IMaximumLoanCalculatorService maxLoanCalculator,
        IEnumerable<IInstallmentPlanStrategy> strategies,
        IValidator<CreateLoanRequest> createValidator)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _creditScoreService = creditScoreService;
        _riskAnalysis = riskAnalysis;
        _interestCalculation = interestCalculation;
        _maxLoanCalculator = maxLoanCalculator;
        _strategies = strategies;
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
            throw new NotFoundException($"{id} numaralı kredi bulunamadı.");
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

        // Müşteri borç kapasitesi hesabı için kredileri ve taksitleriyle yüklenir.
        var customer = await _customerRepository.GetByIdWithLoansAndInstallmentsAsync(request.CustomerId);
        if (customer is null)
            throw new NotFoundException($"{request.CustomerId} numaralı müşteri bulunamadı.");

        var creditScore = await _creditScoreService.GetCreditScoreAsync(request.CustomerId);
        if (creditScore.CreditScore < 400)
            throw new BusinessRuleException($"Kredi başvurusu reddedildi. Kredi skoru {creditScore.CreditScore}, minimum eşiğin altında.");

        // Risk analizi
        var risk = _riskAnalysis.Analyze(customer, request.PrincipalAmount, request.Term, creditScore.CreditScore);
        if (risk.Category == RiskCategory.VeryHigh)
            throw new BusinessRuleException("Kredi başvurusu reddedildi. Müşterinin risk profili çok yüksek.");

        // Maksimum tutar doğrulama
        var maxLoan = _maxLoanCalculator.Calculate(customer, risk.Category, request.Term);
        if (request.PrincipalAmount < MinLoanAmount)
            throw new BusinessRuleException($"Minimum kredi tutarı {MinLoanAmount:N0} TL'dir.");
        if (maxLoan.MaximumAmount > 0 && request.PrincipalAmount > maxLoan.MaximumAmount)
            throw new BusinessRuleException($"İstenen tutar, bu müşteri için uygun maksimum tutarı ({maxLoan.MaximumAmount:N2} TL) aşmaktadır.");

        if (request.IsBalloonPayment)
        {
            if (request.LoanType != LoanType.Vehicle)
                throw new BusinessRuleException("Balon ödeme yalnızca Araç kredileri için kullanılabilir.");
            if (creditScore.CreditScore < BalloonMinCreditScore)
                throw new BusinessRuleException($"Balon ödeme için en az {BalloonMinCreditScore} kredi skoru gereklidir. Mevcut skor: {creditScore.CreditScore}.");
        }

        // Faiz oranı banka tarafından hesaplanır; istemci tarafından belirlenemez.
        decimal interestRate = _interestCalculation.Calculate(risk.Category, request.Term, request.PrincipalAmount, customer);

        var strategy = _strategies.FirstOrDefault(s => s.SupportsBalloon == request.IsBalloonPayment)
            ?? throw new BusinessRuleException("İstenen ödeme türü için uygun bir taksit planı stratejisi bulunamadı.");

        var loan = new Loan
        {
            CustomerId = request.CustomerId,
            LoanType = request.LoanType,
            PrincipalAmount = request.PrincipalAmount,
            InterestRate = interestRate,
            Term = request.Term,
            StartDate = request.StartDate,
            Status = LoanStatus.Active,
            RemainingPrincipal = request.PrincipalAmount,
            Installments = strategy.Generate(request.PrincipalAmount, interestRate, request.Term, request.StartDate)
        };

        var created = await _loanRepository.AddAsync(loan);
        return MapToResponse(created);
    }

    // Amortizasyon toplam ödemesi: aylık taksit × vade.
    // Taksitler yüklüyse doğrudan topla (balon ödeme için de doğru);
    // yüklü değilse amortizasyon formülüyle yaklaşık hesapla.
    private static decimal ComputeTotalPayable(Loan loan)
    {
        if (loan.Installments.Any())
            return loan.Installments.Sum(i => i.Amount);

        decimal r = loan.InterestRate / 100 / 12;
        if (r == 0 || loan.Term <= 0) return loan.PrincipalAmount;
        double factor = Math.Pow(1 + (double)r, loan.Term);
        decimal monthly = loan.PrincipalAmount * ((decimal)r * (decimal)factor / ((decimal)factor - 1));
        return Math.Round(monthly * loan.Term, 2);
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
        TotalPayableAmount = ComputeTotalPayable(loan),
        Installments = loan.Installments.Select(i => new InstallmentResponse
        {
            Id = i.Id,
            LoanId = i.LoanId,
            InstallmentNumber = i.InstallmentNumber,
            Amount = i.Amount,
            DueDate = i.DueDate,
            Status = i.Status,
            IsBalloon = i.IsBalloon,
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
