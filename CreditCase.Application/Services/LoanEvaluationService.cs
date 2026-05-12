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
/// Kredi değerlendirme orchestrator. claude.md §6A, §19.
/// SRP: yalnızca koordine eder; risk analizi, vade oranı hesabı, maksimum tutar kendi servislerinde.
/// </summary>
public class LoanEvaluationService : ILoanEvaluationService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanEvaluationRepository _evaluationRepository;
    private readonly ICreditScoreService _creditScoreService;
    private readonly IRiskAnalysisService _riskAnalysis;
    private readonly IInterestCalculationService _rateCalculation;
    private readonly IMaximumLoanCalculatorService _maxLoanCalculator;
    private readonly IValidator<LoanApplicationRequest> _validator;

    public LoanEvaluationService(
        ICustomerRepository customerRepository,
        ILoanEvaluationRepository evaluationRepository,
        ICreditScoreService creditScoreService,
        IRiskAnalysisService riskAnalysis,
        IInterestCalculationService rateCalculation,
        IMaximumLoanCalculatorService maxLoanCalculator,
        IValidator<LoanApplicationRequest> validator)
    {
        _customerRepository = customerRepository;
        _evaluationRepository = evaluationRepository;
        _creditScoreService = creditScoreService;
        _riskAnalysis = riskAnalysis;
        _rateCalculation = rateCalculation;
        _maxLoanCalculator = maxLoanCalculator;
        _validator = validator;
    }

    public async Task<LoanEvaluationResponse> EvaluateAsync(LoanApplicationRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var customer = await _customerRepository.GetByIdWithLoansAndInstallmentsAsync(request.CustomerId);
        if (customer is null)
            throw new NotFoundException($"{request.CustomerId} numaralı müşteri bulunamadı.");

        // 1. Dış kredi skoru sorgusu
        var creditScoreResult = await _creditScoreService.GetCreditScoreAsync(request.CustomerId);
        int creditScore = creditScoreResult.CreditScore;

        // 2. Kredi skoru kategorisi (vade oranı ve limitler için)
        var scoreCategory = ScoreCategoryHelper.FromScore(creditScore);

        // 3. Kategori bazlı vade sınırlaması — claude.md §6A
        int maxTermForCategory = ScoreCategoryHelper.MaxTermMonths(scoreCategory);
        int effectiveTerm = Math.Min(request.RequestedTerm, maxTermForCategory);

        // 4. Risk analizi (onay kararı için)
        var riskResult = _riskAnalysis.Analyze(customer, request.RequestedAmount, effectiveTerm, creditScore);
        var riskLevel = ScoreCategoryHelper.ToRiskCategory(scoreCategory);

        // 5. Maksimum tutar hesabı
        var maxLoan = _maxLoanCalculator.Calculate(customer, scoreCategory, effectiveTerm);

        bool isApproved = scoreCategory != ScoreCategory.Kritik
            && riskResult.Category != RiskCategory.VeryHigh
            && maxLoan.MaximumAmount > 0;

        string? rejectionReason = null;
        decimal approvedAmount = 0;
        decimal approvedRate = 0;
        decimal monthlyEstimate = 0;

        if (isApproved)
        {
            approvedAmount = Math.Min(request.RequestedAmount, maxLoan.MaximumAmount);
            approvedRate = _rateCalculation.Calculate(creditScore, request.LoanType, effectiveTerm, customer);
            monthlyEstimate = CalculateMonthlyInstallment(approvedAmount, approvedRate, effectiveTerm);

            // Minimum taksit kontrolü — claude.md §6A
            decimal minInstallment = ScoreCategoryHelper.MinInstallmentAmount(scoreCategory);
            if (monthlyEstimate < minInstallment)
            {
                isApproved = false;
                rejectionReason = $"Tahmini aylık taksit ({monthlyEstimate:N2} TL), " +
                    $"{scoreCategory} kategorisi için minimum taksit tutarının ({minInstallment:N0} TL) altında. " +
                    "Daha yüksek kredi tutarı veya daha kısa vade deneyin.";
                approvedAmount = 0;
                approvedRate = 0;
                monthlyEstimate = 0;
            }
        }

        if (!isApproved && rejectionReason is null)
        {
            rejectionReason = scoreCategory == ScoreCategory.Kritik
                ? $"Kredi skoru {creditScore} — Kritik kategorisi, manuel inceleme gerektirir."
                : riskResult.Category == RiskCategory.VeryHigh
                    ? $"Risk puanı {riskResult.TotalScore:F1}, minimum eşiğin altında."
                    : "Aylık gelir ve mevcut borçlar dikkate alındığında borç kapasitesi yetersiz.";
        }

        decimal debtToIncomeRatio = CalculateDebtToIncomeRatio(customer, request.RequestedAmount, effectiveTerm);

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
            ApprovedRateAmount = approvedRate,
            RiskLevel = riskLevel,
            CreditScore = creditScore,
            DebtToIncomeRatio = debtToIncomeRatio,
            MonthlyInstallmentEstimate = monthlyEstimate,
            RejectionReason = rejectionReason,
            EvaluationDate = DateTime.UtcNow,
            // claude.md §6A — Geçerlilik Süresi: 7 Gün
            ExpirationDate = DateTime.UtcNow.AddDays(7)
        };

        var saved = await _evaluationRepository.AddAsync(evaluation);
        return MapToResponse(saved, customer, scoreCategory);
    }

    public async Task<LoanEvaluationResponse> GetByIdAsync(int evaluationId)
    {
        var evaluation = await _evaluationRepository.GetByIdAsync(evaluationId);
        if (evaluation is null)
            throw new NotFoundException($"{evaluationId} numaralı kredi değerlendirmesi bulunamadı.");
        return MapToResponse(evaluation, evaluation.Customer,
            ScoreCategoryHelper.FromScore(evaluation.CreditScore));
    }

    public async Task<IEnumerable<LoanEvaluationResponse>> GetByCustomerIdAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer is null)
            throw new NotFoundException($"{customerId} numaralı müşteri bulunamadı.");

        var evaluations = await _evaluationRepository.GetByCustomerIdAsync(customerId);
        return evaluations.Select(e => MapToResponse(e, customer,
            ScoreCategoryHelper.FromScore(e.CreditScore)));
    }

    public async Task<LoanEvaluationResponse> GetMaximumEligibilityAsync(int customerId)
    {
        var request = new LoanApplicationRequest
        {
            CustomerId = customerId,
            LoanType = LoanType.Personal,
            RequestedAmount = 1_000_000,
            RequestedTerm = 72   // claude.md §6A maksimum vade
        };
        return await EvaluateAsync(request);
    }

    // ── Vergi sabitleri ───────────────────────────────────────────────────────────
    // Taşıt ve ihtiyaç kredisi standartları (mortgage'da KKDF=0).
    private const decimal KkdfRate = 0.15m;
    private const decimal BsmvRate = 0.05m;

    // ── Yardımcı metotlar ────────────────────────────────────────────────────────

    /// <summary>
    /// Amortisasyon formülü: A = P × r_brüt(1+r_brüt)^n / [(1+r_brüt)^n - 1]
    /// netRateAmount aylık net yüzdedir (örn: 3.91); brüt oran = net × (1 + KKDF + BSMV).
    /// </summary>
    private static decimal CalculateMonthlyInstallment(decimal principal, decimal netRateAmount, int termMonths)
    {
        if (termMonths <= 0 || principal <= 0) return 0;
        decimal grossRate = netRateAmount * (1 + KkdfRate + BsmvRate);
        decimal r = grossRate / 100m;
        if (r == 0) return Math.Round(principal / termMonths, 2);
        double factor = Math.Pow(1 + (double)r, termMonths);
        return Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);
    }

    /// <summary>
    /// Her taksit için Anapara / Net Faiz / KKDF / BSMV dökümünü üretir.
    /// </summary>
    private static List<InstallmentPlanRow> GenerateAmortizationTable(
        decimal principal, decimal netRateAmount, int termMonths)
    {
        decimal grossRate = netRateAmount * (1 + KkdfRate + BsmvRate);
        decimal r = grossRate / 100m;
        decimal monthlyPayment = CalculateMonthlyInstallment(principal, netRateAmount, termMonths);

        var rows = new List<InstallmentPlanRow>(termMonths);
        decimal remaining = principal;

        for (int i = 1; i <= termMonths; i++)
        {
            decimal grossInterest = Math.Round(remaining * r, 2);
            decimal netInterest   = Math.Round(grossInterest / (1 + KkdfRate + BsmvRate), 2);
            decimal kkdf          = Math.Round(netInterest * KkdfRate, 2);
            decimal bsmv          = Math.Round(netInterest * BsmvRate, 2);

            decimal principalPayment;
            decimal totalPayment;

            if (i == termMonths)
            {
                // Son taksitte kalan bakiye sıfırlanır
                principalPayment = remaining;
                totalPayment     = principalPayment + grossInterest;
            }
            else
            {
                principalPayment = Math.Round(monthlyPayment - grossInterest, 2);
                totalPayment     = monthlyPayment;
            }

            remaining = Math.Max(0, Math.Round(remaining - principalPayment, 2));

            rows.Add(new InstallmentPlanRow
            {
                InstallmentNumber = i,
                PrincipalPayment  = principalPayment,
                NetInterest       = netInterest,
                Kkdf              = kkdf,
                Bsmv              = bsmv,
                TotalPayment      = totalPayment,
                RemainingBalance  = remaining,
            });
        }

        return rows;
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

    private static LoanEvaluationResponse MapToResponse(
        LoanEvaluationResult e, Customer customer, ScoreCategory scoreCategory)
    {
        // Vergi türevli alanlar yalnızca onaylı değerlendirmelerde hesaplanır
        decimal grossRate      = 0;
        decimal annualCostRate = 0;
        List<InstallmentPlanRow> plan = [];

        if (e.IsApproved && e.ApprovedRateAmount > 0 && e.ApprovedAmount > 0)
        {
            int effectiveTerm = Math.Min(e.RequestedTerm, ScoreCategoryHelper.MaxTermMonths(scoreCategory));

            grossRate = Math.Round(e.ApprovedRateAmount * (1 + KkdfRate + BsmvRate), 4);
            double rGross = (double)(grossRate / 100m);
            annualCostRate = Math.Round((decimal)(Math.Pow(1 + rGross, 12) - 1) * 100, 2);

            plan = GenerateAmortizationTable(e.ApprovedAmount, e.ApprovedRateAmount, effectiveTerm);
        }

        return new LoanEvaluationResponse
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
            ApprovedRateAmount = e.ApprovedRateAmount,
            RiskLevel = e.RiskLevel,
            CreditScoreCategory = scoreCategory,
            CreditScore = e.CreditScore,
            DebtToIncomeRatio = e.DebtToIncomeRatio,
            MonthlyInstallmentEstimate = e.MonthlyInstallmentEstimate,
            GrossRateAmount = grossRate,
            AnnualCostRate = annualCostRate,
            InstallmentPlan = plan,
            RejectionReason = e.RejectionReason,
            EvaluationDate = e.EvaluationDate,
            ExpirationDate = e.ExpirationDate,
        };
    }
}
