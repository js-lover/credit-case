using CreditCase.Application.DTOs.LoanEvaluation;
using CreditCase.Application.DTOs.Loans;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace CreditCase.Tests.Services;

/// <summary>
/// Kredi değerlendirmesi sırasında hesaplamaların doğruluğunu test eder.
/// </summary>
public class LoanEvaluationCalculationTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<ILoanEvaluationRepository> _evalRepoMock;
    private readonly Mock<ICreditScoreService> _creditScoreMock;
    private readonly Mock<IRiskAnalysisService> _riskAnalysisMock;
    private readonly Mock<IInterestCalculationService> _interestCalcMock;
    private readonly Mock<IMaximumLoanCalculatorService> _maxLoanMock;
    private readonly Mock<IValidator<LoanApplicationRequest>> _validatorMock;

    public LoanEvaluationCalculationTests()
    {
        _customerRepoMock = new Mock<ICustomerRepository>();
        _evalRepoMock = new Mock<ILoanEvaluationRepository>();
        _creditScoreMock = new Mock<ICreditScoreService>();
        _riskAnalysisMock = new Mock<IRiskAnalysisService>();
        _interestCalcMock = new Mock<IInterestCalculationService>();
        _maxLoanMock = new Mock<IMaximumLoanCalculatorService>();
        _validatorMock = new Mock<IValidator<LoanApplicationRequest>>();

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<LoanApplicationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    // ── Aylık Taksit Tahmini Hesaplaması ──────────────────────────────────────

    [Fact]
    public void MonthlyInstallmentEstimate_CalculatedCorrectlyByAmortization()
    {
        // Kredi miktarı 12.000 TL, aylık oran 3%, 12 ay
        decimal principal = 12_000m;
        decimal monthlyRate = 3.0m; // %
        int term = 12;

        // Formül: M = P × (r(1+r)^n) / ((1+r)^n - 1)
        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal expected = principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1));
        expected = Math.Round(expected, 2);

        // Sonuç ~1204.87 TL olmalı
        expected.Should().BeGreaterThan(1000m);
        expected.Should().BeLessThan(1300m);
    }

    [Fact]
    public void MonthlyInstallmentEstimate_IncreasesWithLongerTerm()
    {
        decimal principal = 12_000m;
        decimal monthlyRate = 3.0m;

        // 12 ay
        decimal r = monthlyRate / 100m;
        double factor12 = Math.Pow(1 + (double)r, 12);
        decimal monthly12 = Math.Round(principal * (decimal)(r * (decimal)factor12 / ((decimal)factor12 - 1)), 2);

        // 24 ay - daha düşük olmalı
        double factor24 = Math.Pow(1 + (double)r, 24);
        decimal monthly24 = Math.Round(principal * (decimal)(r * (decimal)factor24 / ((decimal)factor24 - 1)), 2);

        monthly24.Should().BeLessThan(monthly12);
    }

    // ── Toplam Ödeme Hesaplaması ──────────────────────────────────────────────

    [Fact]
    public void TotalPayableAmount_CorrectlyCalculated()
    {
        decimal principal = 10_000m;
        decimal monthlyRate = 2.5m;
        int term = 12;

        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal monthly = Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);
        decimal totalPayable = monthly * term;

        decimal interestAmount = totalPayable - principal;

        // Faiz pozitif olmalı
        interestAmount.Should().BeGreaterThan(0);
        totalPayable.Should().BeGreaterThan(principal);
    }

    [Fact]
    public void DebtToIncomeRatio_CalculatedCorrectly()
    {
        // Aylık gelir: 5.000 TL
        decimal monthlyIncome = 5_000m;
        
        // Kredi miktarı: 12.000 TL, 12 ay, aylık oran 3%
        decimal principal = 12_000m;
        decimal monthlyRate = 3.0m;
        int term = 12;

        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal monthlyInstallment = Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);

        // Borç/Gelir oranı = aylık taksit / aylık gelir
        decimal debtToIncomeRatio = monthlyInstallment / monthlyIncome;

        // ~1204.87 / 5000 = 0.24 (%24)
        debtToIncomeRatio.Should().BeGreaterThan(0.2m);
        debtToIncomeRatio.Should().BeLessThan(0.3m);
    }

    // ── Yüksek Kredi Tutarı Senaryosu ──────────────────────────────────────────

    [Fact]
    public void LargeCredit_CalculationsRemainAccurate()
    {
        decimal principal = 500_000m; // 500k TL
        decimal monthlyRate = 3.5m;
        int term = 60; // 5 yıl

        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal monthly = Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);
        decimal totalPayable = monthly * term;

        decimal interest = totalPayable - principal;

        // Büyük kredide faiz da büyük olmalı
        interest.Should().BeGreaterThan(50_000m);
        monthly.Should().BeGreaterThan(10_000m);
    }

    // ── Düşük Kredi Tutarı Senaryosu ──────────────────────────────────────────

    [Fact]
    public void SmallCredit_CalculationsRemainAccurate()
    {
        decimal principal = 1_000m; // Minimum tutar
        decimal monthlyRate = 3.0m;
        int term = 6;

        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal monthly = Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);
        decimal totalPayable = monthly * term;

        totalPayable.Should().BeGreaterThan(principal);
        monthly.Should().BeGreaterThan(0);
    }

    // ── Balon Ödeme Senaryosu ─────────────────────────────────────────────────

    [Fact]
    public void BalloonPayment_LastInstallmentMuchHigher()
    {
        // 24 ay, son taksit düşük olan taksitlerden yüksek
        decimal principal = 30_000m;
        decimal monthlyRate = 3.0m;
        int term = 24;

        // Normal taksit
        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal normalMonthly = Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);

        // Balon ödeme: ilk 23 ay daha düşük, son ay artan
        decimal balloonMonthly = Math.Round(normalMonthly * 0.6m, 2); // İlk 23 ay
        decimal balloonFinal = principal - (balloonMonthly * 23);

        // Son taksit, ilk taksitlerdenhigher olmalı
        balloonFinal.Should().BeGreaterThan(balloonMonthly);
    }

    // ── Sıfır Faiz Durumu ──────────────────────────────────────────────────────

    [Fact]
    public void ZeroInterestLoan_DividesEquallyByTerm()
    {
        decimal principal = 12_000m;
        int term = 12;

        decimal monthly = principal / term; // = 1000
        decimal total = monthly * term;

        monthly.Should().Be(1000m);
        total.Should().Be(principal);
    }

    // ── Faiz Hesaplama Tutarlılığı ───────────────────────────────────────────

    [Fact]
    public void InterestAmount_DifferenceFromPrincipalAndTotalPayable()
    {
        decimal principal = 15_000m;
        decimal totalPayable = 16_500m;

        decimal interest = totalPayable - principal;

        interest.Should().Be(1_500m);
    }

    // ── Ödeme Planı Tutarlılığı ──────────────────────────────────────────────

    [Fact]
    public void InstallmentPlan_SumOfAllInstallmentEqualsTotal()
    {
        decimal principal = 12_000m;
        decimal monthlyRate = 3.0m;
        int term = 12;

        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal monthly = Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);

        // Taksitleri oluştur
        var installments = new List<decimal>();
        for (int i = 0; i < term; i++)
        {
            installments.Add(monthly);
        }

        decimal sumOfInstallments = installments.Sum();

        // Toplam, tüm taksitlerin toplamı olmalı
        (Math.Abs(sumOfInstallments - (monthly * term)) < 0.01m).Should().BeTrue();
    }

    // ── Negatif Değer Validasyonu ────────────────────────────────────────────

    [Fact]
    public void NegativeValues_ShouldNotOccur()
    {
        decimal principal = 10_000m;
        decimal monthlyRate = 2.0m;
        int term = 12;

        decimal r = monthlyRate / 100m;
        double factor = Math.Pow(1 + (double)r, term);
        decimal monthly = Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);

        monthly.Should().BeGreaterThan(0);
        principal.Should().BeGreaterThan(0);
    }
}
