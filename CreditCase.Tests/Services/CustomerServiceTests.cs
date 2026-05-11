using CreditCase.Application.DTOs.Customers;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Services;
using CreditCase.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace CreditCase.Tests.Services;

/// <summary>
/// CustomerService için birim testleri.
/// Müşteri oluşturma, güncelleme, silme ve sorgulama senaryolarını kapsar.
/// Veritabanı bağlantısı yoktur; tüm bağımlılıklar Mock ile izole edilmiştir.
/// </summary>
public class CustomerServiceTests
{
    // ── Bağımlılık mock'ları ──────────────────────────────────────────────────
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly Mock<IValidator<CreateCustomerRequest>> _validatorMock;

    // Test edilen sınıf (System Under Test)
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();
        _validatorMock = new Mock<IValidator<CreateCustomerRequest>>();

        // ValidateAndThrowAsync, IValidator.ValidateAsync'i dahili olarak çağırır.
        // Varsayılan olarak başarılı sonuç döner (hata listesi boş).
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateCustomerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new CustomerService(_repositoryMock.Object, _validatorMock.Object);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsMatchingCustomerResponse()
    {
        // Arrange — repository'de ID=1 müşteri mevcut
        var customer = BuildCustomer(id: 1, firstName: "Ahmet", lastName: "Yılmaz");
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert — dönen DTO alanları entity ile eşleşmeli
        result.Id.Should().Be(1);
        result.FirstName.Should().Be("Ahmet");
        result.LastName.Should().Be("Yılmaz");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange — repository null döner (kayıt yok)
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.GetByIdAsync(99);

        // Assert — NotFoundException fırlatılmalı
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsCreatedCustomerResponse()
    {
        // Arrange — benzersizlik kontrolleri temiz
        var request = BuildCreateRequest();
        _repositoryMock.Setup(r => r.GetByIdentityNumberAsync(request.IdentityNumber)).ReturnsAsync((Customer?)null);
        _repositoryMock.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((Customer?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => { c.Id = 1; return c; });

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Id.Should().Be(1);
        result.FirstName.Should().Be(request.FirstName);
        result.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateIdentityNumber_ThrowsBusinessRuleException()
    {
        // Arrange — aynı TC kimlik numarasına sahip başka müşteri mevcut
        var request = BuildCreateRequest(identityNumber: "12345678901");
        _repositoryMock
            .Setup(r => r.GetByIdentityNumberAsync("12345678901"))
            .ReturnsAsync(BuildCustomer()); // kayıt var → çakışma

        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert — iş kuralı ihlali: 422 senaryosu
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*identity number*");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsBusinessRuleException()
    {
        // Arrange — TC kimlik numarası boş, e-posta başka müşteride kayıtlı
        var request = BuildCreateRequest(email: "var@example.com");
        _repositoryMock.Setup(r => r.GetByIdentityNumberAsync(It.IsAny<string>())).ReturnsAsync((Customer?)null);
        _repositoryMock
            .Setup(r => r.GetByEmailAsync("var@example.com"))
            .ReturnsAsync(BuildCustomer(id: 99)); // başka müşteride aynı email

        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*email*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.UpdateAsync(99, new UpdateCustomerRequest());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailChangedToExistingEmail_ThrowsBusinessRuleException()
    {
        // Arrange — ID=1 müşteri mevcut, farklı müşteride hedef email kayıtlı
        var existing = BuildCustomer(id: 1, email: "eski@example.com");
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repositoryMock
            .Setup(r => r.GetByEmailAsync("yeni@example.com"))
            .ReturnsAsync(BuildCustomer(id: 99)); // başka müşteride aynı email

        var request = new UpdateCustomerRequest { FirstName = "Ad", LastName = "Soyad", Email = "yeni@example.com", PhoneNumber = "111" };

        // Act
        var act = () => _sut.UpdateAsync(1, request);

        // Assert — email değişti ve çakışıyor → 422
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*email*");
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailUnchanged_DoesNotQueryEmailUniqueness()
    {
        // Arrange — müşteri kendi e-postasını değiştirmeden güncelleme yapıyor
        var existing = BuildCustomer(id: 1, email: "ayni@example.com");
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>())).ReturnsAsync(existing);

        var request = new UpdateCustomerRequest { FirstName = "Ad", LastName = "Soyad", Email = "ayni@example.com", PhoneNumber = "111" };

        // Act
        await _sut.UpdateAsync(1, request);

        // Assert — email değişmedi; GetByEmailAsync hiç çağrılmamalı
        _repositoryMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Customer?)null);

        // Act
        var act = () => _sut.DeleteAsync(99);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_CallsRepositoryDelete()
    {
        // Arrange
        var customer = BuildCustomer(id: 1);
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        // Act
        await _sut.DeleteAsync(1);

        // Assert — repository'nin DeleteAsync'i tam olarak bir kez çağrılmalı
        _repositoryMock.Verify(r => r.DeleteAsync(customer), Times.Once);
    }

    // ── Yardımcı factory metodlar ─────────────────────────────────────────────

    private static Customer BuildCustomer(
        int id = 1,
        string firstName = "Test",
        string lastName = "User",
        string identityNumber = "12345678901",
        string email = "test@example.com") => new()
    {
        Id = id,
        FirstName = firstName,
        LastName = lastName,
        IdentityNumber = identityNumber,
        Email = email,
        PhoneNumber = "5551234567",
        CreatedAt = DateTime.UtcNow
    };

    private static CreateCustomerRequest BuildCreateRequest(
        string firstName = "Test",
        string lastName = "User",
        string identityNumber = "12345678901",
        string email = "test@example.com",
        string phoneNumber = "5551234567") => new()
    {
        FirstName = firstName,
        LastName = lastName,
        IdentityNumber = identityNumber,
        Email = email,
        PhoneNumber = phoneNumber
    };
}
