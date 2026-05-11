using CreditCase.Application.DTOs.Loans;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
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
///   1. Düz faiz (flat-rate) formülünün doğruluğu
///   2. Taksit planının otomatik üretilmesi (sayı, tutar, vadeler)
///   3. Kredi skoru reddi ve müşteri bulunamama hata senaryoları
/// </summary>
public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<ICreditScoreService> _creditScoreServiceMock;
    private readonly Mock<IValidator<CreateLoanRequest>> _validatorMock;
    private readonly LoanService _sut;

    public LoanServiceTests()
    {
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _creditScoreServiceMock = new Mock<ICreditScoreService>();
        _validatorMock = new Mock<IValidator<CreateLoanRequest>>();

        // Varsayılan: validasyon başarılı
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Varsayılan: kredi skoru onaylı
        _creditScoreServiceMock
            .Setup(s => s.GetCreditScoreAsync(It.IsAny<int>()))
            .ReturnsAsync(new CreditScoreResult(1, 1450, "Approved"));

        _sut = new LoanService(
            _loanRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _creditScoreServiceMock.Object,
            _validatorMock.Object);
    }

    // ── Taksit üretimi ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithTwelveMonthTerm_GeneratesTwelveInstallments()
    {
        // Arrange
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(term: 12);
        SetupLoanAdd(request);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert — vade 12 ay ise taksit sayısı tam olarak 12 olmalı
        result.Installments.Should().HaveCount(12);
    }

    [Fact]
    public async Task CreateAsync_FlatRateInterest_CalculatesCorrectMonthlyAmount()
    {
        // Arrange
        // Formül: totalAmount = principal × (1 + rate/100 × termYears)
        //         monthly     = Round(totalAmount / term, 2)
        // Örnek: 12.000 TL, %12 yıllık, 12 ay
        //   termYears   = 12/12   = 1,0
        //   totalAmount = 12000 × (1 + 0,12 × 1) = 13.440
        //   monthly     = 13440 / 12 = 1.120,00
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(principal: 12000m, interestRate: 12m, term: 12);
        SetupLoanAdd(request);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Installments.Should().AllSatisfy(i => i.Amount.Should().Be(1120.00m));
    }

    [Fact]
    public async Task CreateAsync_FlatRateInterest_SixMonthTerm_CalculatesCorrectMonthlyAmount()
    {
        // Arrange
        // 6.000 TL, %10 yıllık, 6 ay
        //   termYears   = 6/12     = 0,5
        //   totalAmount = 6000 × (1 + 0,10 × 0,5) = 6000 × 1,05 = 6.300
        //   monthly     = 6300 / 6 = 1.050,00
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(principal: 6000m, interestRate: 10m, term: 6);
        SetupLoanAdd(request);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Installments.Should().AllSatisfy(i => i.Amount.Should().Be(1050.00m));
    }

    [Fact]
    public async Task CreateAsync_AllGeneratedInstallments_HaveUnpaidStatus()
    {
        // Arrange — oluşturulan tüm taksitler Unpaid başlamalı
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(term: 6);
        SetupLoanAdd(request);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Installments.Should().AllSatisfy(i =>
            i.Status.Should().Be(InstallmentStatus.Unpaid));
    }

    [Fact]
    public async Task CreateAsync_GeneratedInstallments_DueDatesIncrementMonthly()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(term: 3, startDate: startDate);
        SetupLoanAdd(request);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert — 3 aylık kredi: vadeler Şubat, Mart, Nisan olmalı
        result.Installments[0].DueDate.Should().Be(startDate.AddMonths(1));
        result.Installments[1].DueDate.Should().Be(startDate.AddMonths(2));
        result.Installments[2].DueDate.Should().Be(startDate.AddMonths(3));
    }

    [Fact]
    public async Task CreateAsync_NewLoan_RemainingPrincipalEqualsFullPrincipal()
    {
        // Arrange — kredi oluşturulduğunda kalan anapara tam ana para olmalı
        SetupCustomerFound(customerId: 1);
        var request = BuildLoanRequest(principal: 10000m);
        SetupLoanAdd(request);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.RemainingPrincipal.Should().Be(10000m);
    }

    // ── Hata senaryoları ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNonExistingCustomer_ThrowsNotFoundException()
    {
        // Arrange — müşteri bulunamıyor
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.CreateAsync(BuildLoanRequest(customerId: 999));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task CreateAsync_WithRejectedCreditScore_ThrowsBusinessRuleException()
    {
        // Arrange — kredi skoru servisi "Rejected" döndürüyor
        SetupCustomerFound(customerId: 1);
        _creditScoreServiceMock
            .Setup(s => s.GetCreditScoreAsync(1))
            .ReturnsAsync(new CreditScoreResult(1, 300, "Rejected"));

        // Act
        var act = () => _sut.CreateAsync(BuildLoanRequest(customerId: 1));

        // Assert — reddedilmiş başvuru iş kuralı ihlalidir
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Rejected*");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _loanRepositoryMock.Setup(r => r.GetByIdWithInstallmentsAsync(99)).ReturnsAsync((Loan?)null);

        // Act
        var act = () => _sut.GetByIdAsync(99);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    // ── Yardımcı metodlar ─────────────────────────────────────────────────────

    private void SetupCustomerFound(int customerId)
    {
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(customerId))
            .ReturnsAsync(new Customer { Id = customerId, FirstName = "Test", LastName = "User" });
    }

    /// <summary>
    /// AddAsync mock'u: eklenen Loan nesnesini olduğu gibi geri döndürür.
    /// Installments koleksiyonu zaten dolu olduğu için MapToResponse çalışır.
    /// </summary>
    private void SetupLoanAdd(CreateLoanRequest request)
    {
        _loanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Loan>()))
            .ReturnsAsync((Loan l) =>
            {
                l.Id = 1;
                // Navigation property'lerin LoanId'si servis tarafından doldurulmaz;
                // MapToResponse'un çalışması için manuel set ediyoruz.
                foreach (var i in l.Installments) i.LoanId = 1;
                return l;
            });
    }

    private static CreateLoanRequest BuildLoanRequest(
        int customerId = 1,
        decimal principal = 12000m,
        decimal interestRate = 12m,
        int term = 12,
        DateTime? startDate = null) => new()
    {
        CustomerId = customerId,
        LoanType = LoanType.Personal,
        PrincipalAmount = principal,
        InterestRate = interestRate,
        Term = term,
        StartDate = startDate ?? new DateTime(2026, 1, 1)
    };
}
