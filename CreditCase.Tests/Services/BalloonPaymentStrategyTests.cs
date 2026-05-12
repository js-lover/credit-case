using CreditCase.Application.DTOs.Loans;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Application.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using CreditCase.Infrastructure.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace CreditCase.Tests.Services;

/// <summary>
/// BalloonPaymentStrategy birim testleri.
///
/// Kapsam:
///   - Taksit planı yapısı (adet, IsBalloon, sıra numaraları, durum)
///   - Vade tarihleri
///   - Tutar hesaplaması (regular = 0.60 × normalMonthly, balon tutarlılığı)
///   - MaxBalloonTerm (> 36 ay → exception)
///   - MaxBalloonRatio (aşırı oran → exception)
///   - LoanService entegrasyonu (strateji yönlendirme, iş kuralları)
/// </summary>
public class BalloonPaymentStrategyTests
{
    private readonly BalloonPaymentStrategy _sut = new();

    private const decimal DefaultPrincipal = 100_000m;
    private const decimal DefaultRate      = 3.30m;       // Vehicle / Dengeli / 12 ay
    private const int     DefaultTerm      = 12;
    private static readonly DateTime DefaultStart = new(2026, 1, 1);

    // ── Yapısal doğrulama ─────────────────────────────────────────────────────

    [Fact]
    public void SupportsBalloon_IsTrue()
    {
        _sut.SupportsBalloon.Should().BeTrue();
    }

    [Fact]
    public void Generate_ProducesExactlyTermInstallments()
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.Should().HaveCount(DefaultTerm);
    }

    [Fact]
    public void Generate_LastInstallment_IsBalloonTrue()
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.Last().IsBalloon.Should().BeTrue();
    }

    [Fact]
    public void Generate_AllExceptLastInstallment_IsBalloonFalse()
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.SkipLast(1).Should().AllSatisfy(i => i.IsBalloon.Should().BeFalse());
    }

    [Fact]
    public void Generate_InstallmentNumbers_AreSequentialStartingAtOne()
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.Select(i => i.InstallmentNumber)
            .Should().Equal(Enumerable.Range(1, DefaultTerm));
    }

    [Fact]
    public void Generate_AllInstallments_HaveUnpaidStatus()
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.Should().AllSatisfy(i => i.Status.Should().Be(InstallmentStatus.Unpaid));
    }

    // ── Vade tarihleri ────────────────────────────────────────────────────────

    [Fact]
    public void Generate_DueDates_IncrementMonthlyFromStartDate()
    {
        var start  = new DateTime(2026, 3, 15);
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, start);

        for (int i = 0; i < result.Count; i++)
            result[i].DueDate.Should().Be(start.AddMonths(i + 1),
                because: $"taksit {i + 1} başlangıçtan {i + 1} ay sonra olmalıdır");
    }

    [Fact]
    public void Generate_LastInstallment_DueDateEqualToStartPlusTerm()
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.Last().DueDate.Should().Be(DefaultStart.AddMonths(DefaultTerm));
    }

    // ── Tutar hesaplaması ─────────────────────────────────────────────────────

    [Fact]
    public void Generate_RegularInstallments_AmountEqualsRoundedSixtyPercentOfNormalMonthly()
    {
        decimal normalMonthly   = StandardInstallmentStrategy.ComputeMonthly(DefaultPrincipal, DefaultRate, DefaultTerm);
        decimal expectedRegular = Math.Round(normalMonthly * 0.60m, 2);

        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.SkipLast(1).Should().AllSatisfy(i =>
            i.Amount.Should().Be(expectedRegular,
                because: "normal taksit, standart aylık ödemenin %60'ı olmalıdır"));
    }

    [Fact]
    public void Generate_RegularInstallments_AmountIsLessThanStandardMonthly()
    {
        decimal normalMonthly = StandardInstallmentStrategy.ComputeMonthly(DefaultPrincipal, DefaultRate, DefaultTerm);

        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.First().Amount.Should().BeLessThan(normalMonthly,
            because: "balon planda aylık ödemeler standart plandan küçük olmalıdır");
    }

    [Fact]
    public void Generate_BalloonInstallment_IsLargerThanRegularInstallments()
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        decimal regularAmount = result.First().Amount;
        decimal balloonAmount = result.Last().Amount;

        balloonAmount.Should().BeGreaterThan(regularAmount,
            because: "son balon taksit, ertelenen borcu kapsamalıdır");
    }

    [Fact]
    public void Generate_TotalPayable_EqualsStandardAmortizationTotal()
    {
        // balloonAmount = total − regularAmount × (term−1)  →  toplam ödeme standart planla eşit
        decimal normalMonthly = StandardInstallmentStrategy.ComputeMonthly(DefaultPrincipal, DefaultRate, DefaultTerm);
        decimal expectedTotal = normalMonthly * DefaultTerm;

        var result     = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);
        decimal actual = result.Sum(i => i.Amount);

        actual.Should().BeApproximately(expectedTotal, precision: 0.02m,
            because: "yuvarlama nedeniyle 2 kuruşluk sapma kabul edilebilir");
    }

    [Fact]
    public void Generate_BalloonAmount_IsBelowMaxBalloonRatio()
    {
        // Tüm geçerli senaryolarda balon tutar, anaparanın %90'ını aşmamalıdır
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, DefaultTerm, DefaultStart);

        result.Last().Amount.Should().BeLessThanOrEqualTo(DefaultPrincipal * 0.90m);
    }

    // ── Farklı vade senaryoları (Theory) ─────────────────────────────────────

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(36)]
    public void Generate_ValidTerms_ProduceCorrectInstallmentCount(int term)
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, term, DefaultStart);

        result.Should().HaveCount(term);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(36)]
    public void Generate_ValidTerms_BalloonAmountRemainsWithinNinetyPercentOfPrincipal(int term)
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, term, DefaultStart);

        result.Last().Amount.Should().BeLessThanOrEqualTo(DefaultPrincipal * 0.90m);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(36)]
    public void Generate_ValidTerms_OnlyLastInstallmentIsMarkedBalloon(int term)
    {
        var result = _sut.Generate(DefaultPrincipal, DefaultRate, term, DefaultStart);

        result.Where(i => i.IsBalloon).Should().HaveCount(1);
        result.Last().IsBalloon.Should().BeTrue();
    }

    // ── MaxBalloonTerm aşım hatası ────────────────────────────────────────────

    [Fact]
    public void Generate_WithTermExactlyAtLimit_DoesNotThrow()
    {
        var act = () => _sut.Generate(DefaultPrincipal, DefaultRate, term: 36, DefaultStart);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_WithTermOneAboveLimit_ThrowsBusinessRuleException()
    {
        var act = () => _sut.Generate(DefaultPrincipal, DefaultRate, term: 37, DefaultStart);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*36*");
    }

    [Theory]
    [InlineData(37)]
    [InlineData(48)]
    [InlineData(60)]
    [InlineData(72)]
    public void Generate_WithTermAboveMaxBalloonTerm_ThrowsBusinessRuleException(int term)
    {
        var act = () => _sut.Generate(DefaultPrincipal, DefaultRate, term, DefaultStart);

        act.Should().Throw<BusinessRuleException>();
    }

    // ── MaxBalloonRatio aşım hatası ───────────────────────────────────────────

    [Fact]
    public void Generate_WithExtremelyHighRate_ThrowsBusinessRuleExceptionForRatioViolation()
    {
        // rateAmount=50 → grossRate=60 → r=0.60 → aylık taksit ≈ anapara değeri
        // balloonAmount >> principalAmount × 0.90 → exception fırlatılmalı
        var act = () => _sut.Generate(DefaultPrincipal, rateAmount: 50m, term: 6, DefaultStart);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*%90*");
    }
}

/// <summary>
/// LoanService üzerinden balon ödeme iş kurallarının entegrasyon testleri.
///
/// Kapsam:
///   - Balon talep edildiğinde BalloonPaymentStrategy seçilmesi
///   - Balon talep edilmediğinde StandardInstallmentStrategy seçilmesi
///   - Kredi skoru < 750 → red
///   - Araç dışı kredi türü → red
///   - Oluşturulan planda son taksitin IsBalloon = true olması
/// </summary>
public class BalloonLoanServiceTests
{
    private readonly Mock<ILoanRepository>               _loanRepo;
    private readonly Mock<ICustomerRepository>           _customerRepo;
    private readonly Mock<ICreditScoreService>           _creditScoreSvc;
    private readonly Mock<IRiskAnalysisService>          _riskAnalysis;
    private readonly Mock<IInterestCalculationService>   _interestCalc;
    private readonly Mock<IMaximumLoanCalculatorService> _maxLoan;
    private readonly Mock<IValidator<CreateLoanRequest>> _validator;
    private readonly Mock<IInstallmentPlanStrategy>      _standardMock;
    private readonly Mock<IInstallmentPlanStrategy>      _balloonMock;

    public BalloonLoanServiceTests()
    {
        _loanRepo       = new Mock<ILoanRepository>();
        _customerRepo   = new Mock<ICustomerRepository>();
        _creditScoreSvc = new Mock<ICreditScoreService>();
        _riskAnalysis   = new Mock<IRiskAnalysisService>();
        _interestCalc   = new Mock<IInterestCalculationService>();
        _maxLoan        = new Mock<IMaximumLoanCalculatorService>();
        _validator      = new Mock<IValidator<CreateLoanRequest>>();

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _riskAnalysis
            .Setup(s => s.Analyze(It.IsAny<Customer>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new RiskAnalysisResult(RiskCategory.Low, 80m));

        _interestCalc
            .Setup(s => s.Calculate(It.IsAny<int>(), It.IsAny<LoanType>(), It.IsAny<int>(), It.IsAny<Customer>()))
            .Returns(3.30m);

        _maxLoan
            .Setup(s => s.Calculate(It.IsAny<Customer>(), It.IsAny<ScoreCategory>(), It.IsAny<int>()))
            .Returns(new MaximumLoanResult(1_000_000m, 60));

        _standardMock = new Mock<IInstallmentPlanStrategy>();
        _standardMock.Setup(s => s.SupportsBalloon).Returns(false);
        _standardMock
            .Setup(s => s.Generate(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns((decimal p, decimal r, int t, DateTime start) =>
                Enumerable.Range(1, t).Select(i => new Installment
                {
                    InstallmentNumber = i, Amount = 1000m,
                    DueDate = start.AddMonths(i), Status = InstallmentStatus.Unpaid,
                    IsBalloon = false
                }).ToList());

        _balloonMock = new Mock<IInstallmentPlanStrategy>();
        _balloonMock.Setup(s => s.SupportsBalloon).Returns(true);
        _balloonMock
            .Setup(s => s.Generate(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns((decimal p, decimal r, int t, DateTime start) =>
            {
                var list = Enumerable.Range(1, t - 1).Select(i => new Installment
                {
                    InstallmentNumber = i, Amount = 600m,
                    DueDate = start.AddMonths(i), Status = InstallmentStatus.Unpaid,
                    IsBalloon = false
                }).ToList();
                list.Add(new Installment
                {
                    InstallmentNumber = t, Amount = 50_000m,
                    DueDate = start.AddMonths(t), Status = InstallmentStatus.Unpaid,
                    IsBalloon = true
                });
                return list;
            });
    }

    // ── Strateji yönlendirme ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithBalloonFalse_UsesStandardStrategy()
    {
        SetupHighScoreCustomer(1);
        SetupLoanSave();
        var sut     = BuildService();
        var request = BuildVehicleRequest(isBalloon: false);

        await sut.CreateAsync(request);

        _standardMock.Verify(
            s => s.Generate(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>()),
            Times.Once);
        _balloonMock.Verify(
            s => s.Generate(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithBalloonTrue_UsesBalloonStrategy()
    {
        SetupHighScoreCustomer(1);
        SetupLoanSave();
        var sut     = BuildService();
        var request = BuildVehicleRequest(isBalloon: true);

        await sut.CreateAsync(request);

        _balloonMock.Verify(
            s => s.Generate(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>()),
            Times.Once);
        _standardMock.Verify(
            s => s.Generate(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    // ── Balon planda son taksit işareti ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithBalloonTrue_ResultLastInstallmentIsBalloonTrue()
    {
        SetupHighScoreCustomer(1);
        SetupLoanSave();
        var sut    = BuildService();
        var result = await sut.CreateAsync(BuildVehicleRequest(isBalloon: true));

        result.Installments.Last().IsBalloon.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithBalloonFalse_NoInstallmentIsMarkedBalloon()
    {
        SetupHighScoreCustomer(1);
        SetupLoanSave();
        var sut    = BuildService();
        var result = await sut.CreateAsync(BuildVehicleRequest(isBalloon: false));

        result.Installments.Should().AllSatisfy(i => i.IsBalloon.Should().BeFalse());
    }

    // ── İş kuralı hataları ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_BalloonWithLowCreditScore_ThrowsBusinessRuleException()
    {
        // Skor 1000: Kritik değil (≥ 970) ama Dengeli minimum (1150) altında → balon reddedilir
        SetupCustomerWithScore(1, score: 1000);
        var sut = BuildService();
        var act = () => sut.CreateAsync(BuildVehicleRequest(isBalloon: true));

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*1150*");
    }

    [Fact]
    public async Task CreateAsync_BalloonWithPersonalLoanType_ThrowsBusinessRuleException()
    {
        SetupHighScoreCustomer(1);
        var sut = BuildService();
        var act = () => sut.CreateAsync(new CreateLoanRequest
        {
            CustomerId      = 1,
            LoanType        = LoanType.Personal, // Araç değil
            PrincipalAmount = 100_000m,
            Term            = 12,
            StartDate       = new DateTime(2026, 1, 1),
            IsBalloonPayment = true
        });

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Araç*");
    }

    [Fact]
    public async Task CreateAsync_BalloonWithEducationLoanType_ThrowsBusinessRuleException()
    {
        SetupHighScoreCustomer(1);
        var sut = BuildService();
        var act = () => sut.CreateAsync(new CreateLoanRequest
        {
            CustomerId      = 1,
            LoanType        = LoanType.Education,
            PrincipalAmount = 100_000m,
            Term            = 12,
            StartDate       = new DateTime(2026, 1, 1),
            IsBalloonPayment = true
        });

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Araç*");
    }

    [Fact]
    public async Task CreateAsync_BalloonWithMinimumRequiredScore_Succeeds()
    {
        SetupCustomerWithScore(1, score: 1150); // Dengeli alt sınırı — tam eşik
        SetupLoanSave();
        var sut = BuildService();
        var act = () => sut.CreateAsync(BuildVehicleRequest(isBalloon: true));

        await act.Should().NotThrowAsync();
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────

    private LoanService BuildService() => new(
        _loanRepo.Object,
        _customerRepo.Object,
        _creditScoreSvc.Object,
        _riskAnalysis.Object,
        _interestCalc.Object,
        _maxLoan.Object,
        [_standardMock.Object, _balloonMock.Object],
        _validator.Object);

    private void SetupHighScoreCustomer(int id) =>
        SetupCustomerWithScore(id, score: 1550);

    private void SetupCustomerWithScore(int id, int score)
    {
        _customerRepo
            .Setup(r => r.GetByIdWithLoansAndInstallmentsAsync(id))
            .ReturnsAsync(new Customer
            {
                Id = id, FirstName = "Test", LastName = "User",
                MonthlyIncome = 50_000m, Loans = []
            });

        _creditScoreSvc
            .Setup(s => s.GetCreditScoreAsync(id))
            .ReturnsAsync(new CreditScoreResult(id, score, "Low", [], 0.05m, DateTime.UtcNow));
    }

    private void SetupLoanSave() =>
        _loanRepo
            .Setup(r => r.AddAsync(It.IsAny<Loan>()))
            .ReturnsAsync((Loan l) =>
            {
                l.Id = 1;
                foreach (var i in l.Installments) i.LoanId = 1;
                return l;
            });

    private static CreateLoanRequest BuildVehicleRequest(bool isBalloon) => new()
    {
        CustomerId       = 1,
        LoanType         = LoanType.Vehicle,
        PrincipalAmount  = 100_000m,
        Term             = 12,
        StartDate        = new DateTime(2026, 1, 1),
        IsBalloonPayment = isBalloon
    };
}
