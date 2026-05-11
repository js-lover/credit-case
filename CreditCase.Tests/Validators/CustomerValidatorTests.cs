using CreditCase.Application.DTOs.Customers;
using CreditCase.Application.Validators;
using FluentAssertions;

namespace CreditCase.Tests.Validators;

/// <summary>
/// CreateCustomerRequestValidator ve UpdateCustomerRequestValidator için birim testleri.
/// Gerçek validator sınıfları kullanılır; mock bağımlılık yoktur.
/// </summary>
public class CustomerValidatorTests
{
    private readonly CreateCustomerRequestValidator _createValidator = new();
    private readonly UpdateCustomerRequestValidator _updateValidator = new();

    // ── CreateCustomerRequestValidator — IdentityNumber ──────────────────────

    [Fact]
    public async Task IdentityNumber_WithLetters_FailsValidation()
    {
        // Arrange
        var request = BuildCreateRequest(identityNumber: "ABCDEFGHIJK");

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdentityNumber");
    }

    [Fact]
    public async Task IdentityNumber_WithLessThan11Digits_FailsValidation()
    {
        // Arrange
        var request = BuildCreateRequest(identityNumber: "1234567");

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdentityNumber");
    }

    [Fact]
    public async Task IdentityNumber_WithExactly11Digits_PassesValidation()
    {
        // Arrange
        var request = BuildCreateRequest(identityNumber: "12345678901");

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert — IdentityNumber alanı için hata olmamalı
        result.Errors.Should().NotContain(e => e.PropertyName == "IdentityNumber");
    }

    // ── CreateCustomerRequestValidator — PhoneNumber ──────────────────────────

    [Fact]
    public async Task PhoneNumber_WithLetters_FailsValidation()
    {
        // Arrange
        var request = BuildCreateRequest(phoneNumber: "abc");

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public async Task PhoneNumber_WithLessThan10Digits_FailsValidation()
    {
        // Arrange
        var request = BuildCreateRequest(phoneNumber: "555");

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public async Task PhoneNumber_With10Digits_PassesValidation()
    {
        // Arrange
        var request = BuildCreateRequest(phoneNumber: "5551234567");

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public async Task PhoneNumber_With11Digits_PassesValidation()
    {
        // Arrange — 0 ile başlayan 11 haneli numara
        var request = BuildCreateRequest(phoneNumber: "05551234567");

        // Act
        var result = await _createValidator.ValidateAsync(request);

        // Assert
        result.Errors.Should().NotContain(e => e.PropertyName == "PhoneNumber");
    }

    // ── UpdateCustomerRequestValidator — PhoneNumber ──────────────────────────

    [Fact]
    public async Task UpdateValidator_PhoneNumber_WithInvalidFormat_FailsValidation()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            FirstName = "Ad",
            LastName = "Soyad",
            Email = "test@example.com",
            PhoneNumber = "123"
        };

        // Act
        var result = await _updateValidator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    // ── Yardımcı factory metod ────────────────────────────────────────────────

    private static CreateCustomerRequest BuildCreateRequest(
        string identityNumber = "12345678901",
        string phoneNumber = "5551234567") => new()
    {
        FirstName = "Test",
        LastName = "User",
        IdentityNumber = identityNumber,
        Email = "test@example.com",
        PhoneNumber = phoneNumber
    };
}
