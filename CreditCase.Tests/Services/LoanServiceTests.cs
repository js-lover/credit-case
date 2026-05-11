using CreditCase.Application.DTOs.Loans;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Application.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace CreditCase.Tests.Services;

/// <summary>
/// LoanService için birim testleri.
/// Odak noktaları:
///   1. Taksit planının doğru sayı ve durum bilgisiyle üretilmesi
///   2. Kredi skoru, risk ve tutar sınırı reddedilmesi
///   3. Müşteri bulunamama hata senaryoları
/// </summary>
public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<ICreditScoreService> _creditScoreServiceMock;
    private readonly Mock<IRiskAnalysisService> _riskAnalysisMock;
    private readonly Mock<IInterestCalculationService> _interestCalcMock;
    private readonly Mock<IMaximumLoanCalculatorService> _maxLoanMock;
    private readonly Mock<IValidator<CreateLoanRequest>> _validatorMock;
    private readonly LoanService _sut;

    public LoanServiceTests()
    {
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _creditScoreServiceMock = new Mock<ICreditScoreService>();
        _riskAnalysisMock = new Mock<IRiskAnalysisService>();
        _interestCalcMock = new Mock<IInterestCalculationService>();
        _maxLoanMock = new Mock<IMaximumLoanCalculatorService>();
        _validatorMock = new Mock<IValidator<CreateLoanRequest>>();

        // Varsayılan: validasyon başarılı
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Varsayılan: kredi skoru düşük riskli
        _creditScoreServiceMock
            .Setup(s => s.GetCreditScoreAsync(It.IsAny<int>()))
            .ReturnsAsync(new CreditScoreResult(1, 820, "Low", Array.Empty<NegativeRecord>(), 0.05m, DateTime.UtcNow));

        // Varsayılan: düşük risk, faiz %6
        _riskAnalysisMock
            .Setup(s => s.Analyze(It.IsAny<Customer>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new RiskAnalysisResult(RiskCategory.Low, 80m));

        _interestCalcMock
            .Setup(s => s.Calculate(It.IsAny<RiskCategory>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<Customer>()))
            .Returns(6m);

        // Varsayılan: maksimum tutar 500.000 TL
        _maxLoanMock
            .Setup(s => s.Calculate(It.IsAny<Customer>(), It.IsAny<RiskCategory>(), It.IsAny<int>()))
            .Returns(new MaximumLoanResult(500_000m, 120));

        // Standard strategy mock: düz faiz ile taksit üretir
        var standardStrategyMock = new Mock<IInstallmentPlanStrategy>();
        standardStrategyMock.Setup(s => s.SupportsBalloon).Returns(false);
        standardStrategyMock
            .Setup(s => s.Generate(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns((decimal principal, decimal rate, int term, DateTime start) =>
            {
                decimal termYears = term / 12m;
                decimal totalAmount = principal * (1 + rate / 100 * termYears);
                decimal monthlyAmount = Math.Round(totalAmount / term, 2);
                return Enumerable.Range(1, term).Select(i => new Installment
                {
                    InstallmentNumber = i,
                    Amount = monthlyAmount,
                    DueDate = start.AddMonths(i),
                    Status = InstallmentStatus.Unpaid
                }).ToList();
            });

        _sut = new LoanService(
            _loanRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _creditScoreServiceMock.Object,
            _riskAnalysisMock.Object,
            _interestCalcMock.Object,
            _maxLoanMock.Object,
            [standardStrategyMock.Object],
            _validatorMock.Object);
    }

    // ── Taksit üretimi ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithTwelveMonthTerm_GeneratesTwelveInstallments()
    {
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(term: 12);
        SetupLoanAdd(request);

        var result = await _sut.CreateAsync(request);

        result.Installments.Should().HaveCount(12);
    }

    [Fact]
    public async Task CreateAsync_CalculatesMonthlyAmountUsingServerInterestRate()
    {
        // Server %6 faiz döndürüyor; 12.000 TL, 12 ay düz faiz
        // totalAmount = 12000 × (1 + 0.06 × 1) = 12720; monthly = 12720 / 12 = 1060
        _interestCalcMock
            .Setup(s => s.Calculate(It.IsAny<RiskCategory>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<Customer>()))
            .Returns(6m);

        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(principal: 12000m, term: 12);
        SetupLoanAdd(request);

        var result = await _sut.CreateAsync(request);

        result.Installments.Should().AllSatisfy(i => i.Amount.Should().Be(1060.00m));
    }

    [Fact]
    public async Task CreateAsync_AllGeneratedInstallments_HaveUnpaidStatus()
    {
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(term: 12);
        SetupLoanAdd(request);

        var result = await _sut.CreateAsync(request);

        result.Installments.Should().AllSatisfy(i =>
            i.Status.Should().Be(InstallmentStatus.Unpaid));
    }

    [Fact]
    public async Task CreateAsync_GeneratedInstallments_DueDatesIncrementMonthly()
    {
        var startDate = new DateTime(2026, 1, 1);
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(term: 3, startDate: startDate);
        SetupLoanAdd(request);

        var result = await _sut.CreateAsync(request);

        result.Installments[0].DueDate.Should().Be(startDate.AddMonths(1));
        result.Installments[1].DueDate.Should().Be(startDate.AddMonths(2));
        result.Installments[2].DueDate.Should().Be(startDate.AddMonths(3));
    }

    [Fact]
    public async Task CreateAsync_NewLoan_RemainingPrincipalEqualsFullPrincipal()
    {
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(principal: 10000m);
        SetupLoanAdd(request);

        var result = await _sut.CreateAsync(request);

        result.RemainingPrincipal.Should().Be(10000m);
    }

    [Fact]
    public async Task CreateAsync_InterestRate_IsSetFromCalculationService()
    {
        _interestCalcMock
            .Setup(s => s.Calculate(It.IsAny<RiskCategory>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<Customer>()))
            .Returns(11.5m);

        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest();
        SetupLoanAdd(request);

        var result = await _sut.CreateAsync(request);

        result.InterestRate.Should().Be(11.5m);
    }

    // ── Hata senaryoları ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNonExistingCustomer_ThrowsNotFoundException()
    {
        _customerRepositoryMock
            .Setup(r => r.GetByIdWithLoansAndInstallmentsAsync(It.IsAny<int>()))
            .ReturnsAsync((Customer?)null);

        var act = () => _sut.CreateAsync(BuildLoanRequest(customerId: 999));

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task CreateAsync_WithRejectedCreditScore_ThrowsBusinessRuleException()
    {
        SetupCustomerFound(customerId: 1);
        _creditScoreServiceMock
            .Setup(s => s.GetCreditScoreAsync(1))
            .ReturnsAsync(new CreditScoreResult(1, 300, "High", Array.Empty<NegativeRecord>(), 0.60m, DateTime.UtcNow));

        var act = () => _sut.CreateAsync(BuildLoanRequest(customerId: 1));

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*reddedildi*");
    }

    [Fact]
    public async Task CreateAsync_WithVeryHighRisk_ThrowsBusinessRuleException()
    {
        SetupCustomerFound(customerId: 1);
        _riskAnalysisMock
            .Setup(s => s.Analyze(It.IsAny<Customer>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new RiskAnalysisResult(RiskCategory.VeryHigh, 20m));

        var act = () => _sut.CreateAsync(BuildLoanRequest(customerId: 1));

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*risk*");
    }

    [Fact]
    public async Task CreateAsync_AmountExceedingMaximum_ThrowsBusinessRuleException()
    {
        SetupCustomerFound(customerId: 1);
        _maxLoanMock
            .Setup(s => s.Calculate(It.IsAny<Customer>(), It.IsAny<RiskCategory>(), It.IsAny<int>()))
            .Returns(new MaximumLoanResult(5_000m, 120));

        var act = () => _sut.CreateAsync(BuildLoanRequest(principal: 50_000m));

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*maksimum*");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        _loanRepositoryMock.Setup(r => r.GetByIdWithInstallmentsAsync(99)).ReturnsAsync((Loan?)null);

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    // ── Yardımcı metodlar ─────────────────────────────────────────────────────

    private void SetupCustomerFound(int customerId)
    {
        var customer = new Customer
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "User",
            MonthlyIncome = 10_000m,
            Loans = new List<Loan>()
        };
        _customerRepositoryMock
            .Setup(r => r.GetByIdWithLoansAndInstallmentsAsync(customerId))
            .ReturnsAsync(customer);
    }

    private void SetupLoanAdd(CreateLoanRequest request)
    {
        _loanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Loan>()))
            .ReturnsAsync((Loan l) =>
            {
                l.Id = 1;
                foreach (var i in l.Installments) i.LoanId = 1;
                return l;
            });
    }

    private static CreateLoanRequest BuildLoanRequest(
        int customerId = 1,
        decimal principal = 12000m,
        int term = 12,
        DateTime? startDate = null) => new()
    {
        CustomerId = customerId,
        LoanType = LoanType.Personal,
        PrincipalAmount = principal,
        Term = term,
        StartDate = startDate ?? new DateTime(2026, 1, 1)
    };
}
