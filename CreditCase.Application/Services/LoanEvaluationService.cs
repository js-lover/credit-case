using CreditCase.Application.DTOs.LoanEvaluation;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using FluentValidation;

namespace CreditCase.Application.Services;

/// <summary>
/// Kredi değerlendirme orchestrator. CLAUDE.md §6A, §19.
/// SRP: yalnızca koordine eder; risk analizi, faiz hesabı, maksimum tutar kendi servislerinde.
/// </summary>
public class LoanEvaluationService : ILoanEvaluationService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanEvaluationRepository _evaluationRepository;
    private readonly ICreditScoreService _creditScoreService;
    private readonly IRiskAnalysisService _riskAnalysis;
    private readonly IInterestCalculationService _interestCalculation;
    private readonly IMaximumLoanCalculatorService _maxLoanCalculator;
    private readonly IValidator<LoanApplicationRequest> _validator;

    public LoanEvaluationService(
        ICustomerRepository customerRepository,
        ILoanEvaluationRepository evaluationRepository,
        ICreditScoreService creditScoreService,
        IRiskAnalysisService riskAnalysis,
        IInterestCalculationService interestCalculation,
        IMaximumLoanCalculatorService maxLoanCalculator,
        IValidator<LoanApplicationRequest> validator)
    {
        _customerRepository = customerRepository;
        _evaluationRepository = evaluationRepository;
        _creditScoreService = creditScoreService;
        _riskAnalysis = riskAnalysis;
        _interestCalculation = interestCalculation;
        _maxLoanCalculator = maxLoanCalculator;
        _validator = validator;
    }

    /// <summary>
    /// Kredi başvurusunu değerlendirir, sonucu persist eder ve döner.
    /// Red durumunda da kayıt oluşturulur; denetim izi için zorunludur.
    /// </summary>
    public async Task<LoanEvaluationResponse> EvaluateAsync(LoanApplicationRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var customer = await _customerRepository.GetByIdWithLoansAndInstallmentsAsync(request.CustomerId);
        if (customer is null)
            throw new NotFoundException($"{request.CustomerId} numaralı müşteri bulunamadı.");

        // 1. Dış kredi skoru sorgusu
        var creditScoreResult = await _creditScoreService.GetCreditScoreAsync(request.CustomerId);
        int creditScore = creditScoreResult.CreditScore;

        // 2. Risk analizi (Rule Engine)
        var riskResult = _riskAnalysis.Analyze(customer, request.RequestedAmount, request.RequestedTerm, creditScore);

        // 3. Maksimum tutar hesabı
        var maxLoan = _maxLoanCalculator.Calculate(customer, riskResult.Category, request.RequestedTerm);

        bool isApproved = riskResult.Category != RiskCategory.VeryHigh && maxLoan.MaximumAmount > 0;
        string? rejectionReason = null;
        decimal approvedAmount = 0;
        decimal approvedRate = 0;
        decimal monthlyEstimate = 0;

        if (isApproved)
        {
            approvedAmount = Math.Min(request.RequestedAmount, maxLoan.MaximumAmount);
            approvedRate = _interestCalculation.Calculate(riskResult.Category, request.RequestedTerm, approvedAmount, customer);
            monthlyEstimate = CalculateMonthlyInstallment(approvedAmount, approvedRate, request.RequestedTerm);
        }
        else
        {
            rejectionReason = riskResult.Category == RiskCategory.VeryHigh
                ? $"Risk puanı {riskResult.TotalScore:F1}, minimum eşiğin altında. Kredi skoru: {creditScore}."
                : "Aylık gelir ve mevcut borçlar dikkate alındığında borç kapasitesi yetersiz.";
        }

        // 4. Değerlendirme kaydını persist et (onay veya red, her iki durumda da)
        decimal debtToIncomeRatio = CalculateDebtToIncomeRatio(customer, request.RequestedAmount, request.RequestedTerm);

        var evaluation = new LoanEvaluationResult
        {
            CustomerId = request.CustomerId,
            RequestedAmount = request.RequestedAmount,
            RequestedTerm = request.RequestedTerm,
            RequestedLoanType = request.LoanType,
            IsApproved = isApproved,
            ApprovedAmount = approvedAmount,
            MaximumAmount = maxLoan.MaximumAmount,
            MaximumTerm = maxLoan.MaximumTerm,
            ApprovedInterestRate = approvedRate,
            RiskLevel = riskResult.Category,
            CreditScore = creditScore,
            DebtToIncomeRatio = debtToIncomeRatio,
            MonthlyInstallmentEstimate = monthlyEstimate,
            RejectionReason = rejectionReason,
            EvaluationDate = DateTime.UtcNow,
            // Onaylanan başvurular 30 gün geçerlidir.
            ExpirationDate = DateTime.UtcNow.AddDays(30)
        };

        var saved = await _evaluationRepository.AddAsync(evaluation);
        return MapToResponse(saved, customer);
    }

    public async Task<LoanEvaluationResponse> GetByIdAsync(int evaluationId)
    {
        var evaluation = await _evaluationRepository.GetByIdAsync(evaluationId);
        if (evaluation is null)
            throw new NotFoundException($"{evaluationId} numaralı kredi değerlendirmesi bulunamadı.");
        return MapToResponse(evaluation, evaluation.Customer);
    }

    public async Task<IEnumerable<LoanEvaluationResponse>> GetByCustomerIdAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer is null)
            throw new NotFoundException($"{customerId} numaralı müşteri bulunamadı.");

        var evaluations = await _evaluationRepository.GetByCustomerIdAsync(customerId);
        return evaluations.Select(e => MapToResponse(e, customer));
    }

    /// <summary>
    /// Müşterinin mevcut profiliyle alabileceği maksimum krediyi simüle eder.
    /// Standart (en iyi senaryo) parametrelerle bir değerlendirme çalıştırır.
    /// </summary>
    public async Task<LoanEvaluationResponse> GetMaximumEligibilityAsync(int customerId)
    {
        var request = new LoanApplicationRequest
        {
            CustomerId = customerId,
            LoanType = LoanType.Personal,
            RequestedAmount = 1_000_000,  // üst sınır — gerçek maks. hesapla bulunacak
            RequestedTerm = 120           // en uzun vade
        };
        return await EvaluateAsync(request);
    }

    // ── Yardımcı metotlar ────────────────────────────────────────────────────────

    /// <summary>
    /// Amortisasyon formülü: A = P × [r(1+r)^n] / [(1+r)^n - 1]
    /// </summary>
    private static decimal CalculateMonthlyInstallment(decimal principal, decimal annualRatePercent, int termMonths)
    {
        if (termMonths <= 0 || principal <= 0) return 0;
        decimal r = annualRatePercent / 100 / 12;
        if (r == 0) return Math.Round(principal / termMonths, 2);
        double factor = Math.Pow(1 + (double)r, termMonths);
        decimal monthly = principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1));
        return Math.Round(monthly, 2);
    }

    private static decimal CalculateDebtToIncomeRatio(Customer customer, decimal requestedAmount, int termMonths)
    {
        if (customer.MonthlyIncome <= 0) return 1m;
        decimal existingDebt = customer.Loans
            .Where(l => l.Status == LoanStatus.Active && l.Installments.Any())
            .Sum(l => l.Installments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .Select(i => i.Amount)
                .DefaultIfEmpty(0)
                .Average());
        decimal newMonthly = termMonths > 0 ? requestedAmount / termMonths : 0;
        return Math.Round((existingDebt + newMonthly) / customer.MonthlyIncome, 4);
    }

    private static LoanEvaluationResponse MapToResponse(LoanEvaluationResult e, Customer customer) => new()
    {
        Id = e.Id,
        CustomerId = e.CustomerId,
        CustomerFullName = $"{customer.FirstName} {customer.LastName}",
        RequestedAmount = e.RequestedAmount,
        RequestedTerm = e.RequestedTerm,
        RequestedLoanType = e.RequestedLoanType,
        IsApproved = e.IsApproved,
        ApprovedAmount = e.ApprovedAmount,
        MaximumAmount = e.MaximumAmount,
        MaximumTerm = e.MaximumTerm,
        ApprovedInterestRate = e.ApprovedInterestRate,
        RiskLevel = e.RiskLevel,
        CreditScore = e.CreditScore,
        DebtToIncomeRatio = e.DebtToIncomeRatio,
        MonthlyInstallmentEstimate = e.MonthlyInstallmentEstimate,
        RejectionReason = e.RejectionReason,
        EvaluationDate = e.EvaluationDate,
        ExpirationDate = e.ExpirationDate
    };
}
