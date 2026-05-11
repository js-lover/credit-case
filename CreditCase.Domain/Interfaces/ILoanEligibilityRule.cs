using CreditCase.Domain.Entities;

namespace CreditCase.Domain.Interfaces;

public record EligibilityResult(bool IsEligible, string? FailureReason);

/// <summary>
/// Kredi uygunluk kural arayüzü. Kural ihlali başvuruyu doğrudan reddeder;
/// risk skorundan bağımsız olarak mutlak bir engel oluşturur.
/// </summary>
public interface ILoanEligibilityRule
{
    EligibilityResult Check(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore);
}
