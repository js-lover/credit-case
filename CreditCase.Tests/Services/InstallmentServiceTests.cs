using CreditCase.Application.DTOs.Installments;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using FluentAssertions;
using Moq;

namespace CreditCase.Tests.Services;

/// <summary>
/// InstallmentService için birim testleri.
/// Odak noktaları:
///   1. GetAllAsync → UpdateOverdueAsync'in önce çağrılması (K-07 kararı)
///   2. GetByIdAsync → bulunamama senaryosu
///   3. UpdateAsync → durum güncelleme ve bulunamama senaryosu
/// </summary>
public class InstallmentServiceTests
{
    private readonly Mock<IInstallmentRepository> _repositoryMock;
    private readonly InstallmentService _sut;

    public InstallmentServiceTests()
    {
        _repositoryMock = new Mock<IInstallmentRepository>();
        _sut = new InstallmentService(_repositoryMock.Object);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_AlwaysCallsUpdateOverdueFirst()
    {
        // Arrange — overdue güncelleme ve taksit listesi mock'u
        _repositoryMock
            .Setup(r => r.UpdateOverdueAsync())
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Installment>());

        // Act
        await _sut.GetAllAsync();

        // Assert — Overdue güncelleme her zaman GetAll'dan önce çağrılmalı (K-07)
        var callOrder = new MockSequence();
        _repositoryMock.InSequence(callOrder).Setup(r => r.UpdateOverdueAsync());
        _repositoryMock.Verify(r => r.UpdateOverdueAsync(), Times.Once);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllInstallments()
    {
        // Arrange — iki taksit döner
        var installments = new List<Installment>
        {
            BuildInstallment(id: 1, InstallmentStatus.Paid),
            BuildInstallment(id: 2, InstallmentStatus.Unpaid)
        };
        _repositoryMock.Setup(r => r.UpdateOverdueAsync()).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(installments);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsInstallmentResponse()
    {
        // Arrange
        var installment = BuildInstallment(id: 5, InstallmentStatus.Unpaid);
        _repositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(installment);

        // Act
        var result = await _sut.GetByIdAsync(5);

        // Assert
        result.Id.Should().Be(5);
        result.Status.Should().Be(InstallmentStatus.Unpaid);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Installment?)null);

        // Act
        var act = () => _sut.GetByIdAsync(99);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ChangesInstallmentStatus()
    {
        // Arrange — mevcut Unpaid taksiti Overdue olarak güncelle
        var installment = BuildInstallment(id: 1, InstallmentStatus.Unpaid);
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(installment);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Installment>()))
            .ReturnsAsync((Installment i) => i);

        var request = new UpdateInstallmentRequest { Status = InstallmentStatus.Overdue };

        // Act
        var result = await _sut.UpdateAsync(1, request);

        // Assert — durumun güncellendiğini doğrula
        result.Status.Should().Be(InstallmentStatus.Overdue);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Installment?)null);

        // Act
        var act = () => _sut.UpdateAsync(99, new UpdateInstallmentRequest { Status = InstallmentStatus.Paid });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    // ── Yardımcı metodlar ─────────────────────────────────────────────────────

    private static Installment BuildInstallment(int id, InstallmentStatus status) => new()
    {
        Id = id,
        LoanId = 1,
        InstallmentNumber = id,
        Amount = 1000m,
        DueDate = DateTime.UtcNow.AddMonths(id),
        Status = status
    };
}
