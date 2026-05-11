using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Exceptions;
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
/// PaymentService için birim testleri.
/// Odak noktaları:
///   1. Başarılı ödeme sonrası taksit ve kredi durumu güncellemeleri
///   2. RemainingPrincipal hesaplama doğruluğu
///   3. Son taksit ödenmesi → kredinin Closed olması
///   4. Tekrar ödeme ve hata senaryolarının reddi
/// </summary>
public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IInstallmentRepository> _installmentRepositoryMock;
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IValidator<CreatePaymentRequest>> _validatorMock;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _installmentRepositoryMock = new Mock<IInstallmentRepository>();
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _validatorMock = new Mock<IValidator<CreatePaymentRequest>>();

        // Varsayılan: validasyon başarılı
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreatePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Varsayılan: aynı taksit için önceki ödeme yok
        _paymentRepositoryMock
            .Setup(r => r.GetByInstallmentIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Payment?)null);

        // Varsayılan: müşteri bulundu, güncelleme başarılı
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Customer { Id = 1, CreditScoreBonus = 0 });
        _customerRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => c);

        _sut = new PaymentService(
            _paymentRepositoryMock.Object,
            _installmentRepositoryMock.Object,
            _loanRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _validatorMock.Object);
    }

    // ── Başarılı ödeme sonrası durum güncellemeleri ───────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidPayment_SetsInstallmentStatusToPaid()
    {
        // Arrange
        var installment = BuildInstallment(id: 1, status: InstallmentStatus.Unpaid);
        var loan = BuildLoan(principalAmount: 12000m, term: 12, installments: [installment]);
        SetupSuccessfulPayment(installment, loan);

        // Act
        await _sut.CreateAsync(new CreatePaymentRequest { InstallmentId = 1, PaymentAmount = 1120m });

        // Assert — taksit güncelleme çağrısında installment.Status = Paid olmalı
        _installmentRepositoryMock.Verify(r =>
            r.UpdateAsync(It.Is<Installment>(i => i.Status == InstallmentStatus.Paid)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidPayment_RecalculatesRemainingPrincipal()
    {
        // Arrange
        // 12.000 TL, 12 taksit; ilk taksit ödeniyor → 11 taksit kaldı
        // RemainingPrincipal = Round(12000 / 12 * 11, 2) = 11.000
        var installment = BuildInstallment(id: 1, status: InstallmentStatus.Unpaid);
        var loan = BuildLoan(
            principalAmount: 12000m,
            term: 12,
            installments: [installment, .. BuildUnpaidInstallments(11, startId: 2)]);
        SetupSuccessfulPayment(installment, loan);

        // Act
        await _sut.CreateAsync(new CreatePaymentRequest { InstallmentId = 1, PaymentAmount = 1120m });

        // Assert — kalan 11 taksit için RemainingPrincipal = 11.000
        _loanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.Is<Loan>(l => l.RemainingPrincipal == 11000m)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenLastInstallmentPaid_SetsLoanStatusToClosed()
    {
        // Arrange — tek taksit kalan kredi; bu taksit ödeniyor
        var installment = BuildInstallment(id: 1, status: InstallmentStatus.Unpaid);
        var loan = BuildLoan(
            principalAmount: 1000m,
            term: 1,
            installments: [installment]); // sadece 1 taksit
        SetupSuccessfulPayment(installment, loan);

        // Act
        await _sut.CreateAsync(new CreatePaymentRequest { InstallmentId = 1, PaymentAmount = 1000m });

        // Assert — tüm taksitler ödendi → kredi Closed olmalı
        _loanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.Is<Loan>(l => l.Status == LoanStatus.Closed)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenLastInstallmentPaid_SetsRemainingPrincipalToZero()
    {
        // Arrange
        var installment = BuildInstallment(id: 1, status: InstallmentStatus.Unpaid);
        var loan = BuildLoan(principalAmount: 1000m, term: 1, installments: [installment]);
        SetupSuccessfulPayment(installment, loan);

        // Act
        await _sut.CreateAsync(new CreatePaymentRequest { InstallmentId = 1, PaymentAmount = 1000m });

        // Assert — kalan taksit 0 → RemainingPrincipal = 0
        _loanRepositoryMock.Verify(r =>
            r.UpdateAsync(It.Is<Loan>(l => l.RemainingPrincipal == 0m)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidPayment_ReturnsPaymentResponse()
    {
        // Arrange
        var installment = BuildInstallment(id: 1, status: InstallmentStatus.Unpaid);
        var loan = BuildLoan(principalAmount: 1000m, term: 1, installments: [installment]);
        SetupSuccessfulPayment(installment, loan);

        var request = new CreatePaymentRequest { InstallmentId = 1, PaymentAmount = 1000m };

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert — dönen response alanları doğru
        result.InstallmentId.Should().Be(1);
        result.PaymentAmount.Should().Be(1000m);
        result.Status.Should().Be(PaymentStatus.Successful);
    }

    // ── Hata senaryoları ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNonExistingInstallment_ThrowsNotFoundException()
    {
        // Arrange — taksit bulunamıyor
        _installmentRepositoryMock
            .Setup(r => r.GetByIdAsync(99))
            .ReturnsAsync((Installment?)null);

        // Act
        var act = () => _sut.CreateAsync(new CreatePaymentRequest { InstallmentId = 99, PaymentAmount = 100m });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task CreateAsync_WithAlreadyPaidInstallment_ThrowsBusinessRuleException()
    {
        // Arrange — taksit zaten Paid durumunda
        var paidInstallment = BuildInstallment(id: 1, status: InstallmentStatus.Paid);
        _installmentRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paidInstallment);

        // Act
        var act = () => _sut.CreateAsync(new CreatePaymentRequest { InstallmentId = 1, PaymentAmount = 100m });

        // Assert — iki kez ödeme yapılamaz
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*zaten ödenmiştir*");
    }

    [Fact]
    public async Task CreateAsync_WithExistingPaymentRecord_ThrowsBusinessRuleException()
    {
        // Arrange — taksit Unpaid ama payments tablosunda kayıt var
        // (kısmi yazım sonrası yarım kalan ödeme senaryosu — K-16 güvencesi)
        var installment = BuildInstallment(id: 1, status: InstallmentStatus.Unpaid);
        _installmentRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(installment);
        _paymentRepositoryMock
            .Setup(r => r.GetByInstallmentIdAsync(1))
            .ReturnsAsync(new Payment { Id = 99, InstallmentId = 1 }); // ödeme kaydı var

        // Act
        var act = () => _sut.CreateAsync(new CreatePaymentRequest { InstallmentId = 1, PaymentAmount = 100m });

        // Assert — ikinci guard devreye girmeli
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*zaten bir ödeme kaydı*");
    }

    // ── Yardımcı metodlar ─────────────────────────────────────────────────────

    /// <summary>
    /// Başarılı ödeme senaryosu için gerekli tüm mock kurulumlarını yapar.
    /// </summary>
    private void SetupSuccessfulPayment(Installment installment, Loan loan)
    {
        _installmentRepositoryMock
            .Setup(r => r.GetByIdAsync(installment.Id))
            .ReturnsAsync(installment);

        _installmentRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Installment>()))
            .ReturnsAsync((Installment i) => i);

        // Ödeme sonrası kredi sorgusu tüm taksitleri içermeli
        _loanRepositoryMock
            .Setup(r => r.GetByIdWithInstallmentsAsync(loan.Id))
            .ReturnsAsync(loan);

        _loanRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
            .ReturnsAsync((Loan l) => l);

        // Yeni ödeme kaydını geri döndür
        _paymentRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => { p.Id = 100; return p; });
    }

    private static Installment BuildInstallment(int id, InstallmentStatus status, int loanId = 1) => new()
    {
        Id = id,
        LoanId = loanId,
        InstallmentNumber = id,
        Amount = 1000m,
        DueDate = DateTime.UtcNow.AddMonths(id),
        Status = status
    };

    private static Loan BuildLoan(decimal principalAmount, int term, List<Installment> installments) => new()
    {
        Id = 1,
        CustomerId = 1,
        PrincipalAmount = principalAmount,
        Term = term,
        Status = LoanStatus.Active,
        RemainingPrincipal = principalAmount,
        Installments = installments
    };

    /// <summary>
    /// Belirtilen sayıda Unpaid taksit listesi üretir.
    /// Paid durumuna getirilen taksitle birlikte kalan taksitleri simüle etmek için kullanılır.
    /// </summary>
    private static List<Installment> BuildUnpaidInstallments(int count, int startId) =>
        Enumerable.Range(startId, count)
            .Select(i => BuildInstallment(id: i, status: InstallmentStatus.Unpaid))
            .ToList();
}
