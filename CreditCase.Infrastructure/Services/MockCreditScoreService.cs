using CreditCase.Application.Interfaces.External;

namespace CreditCase.Infrastructure.Services;

public class MockCreditScoreService : ICreditScoreService
{
    public Task<CreditScoreResult> GetCreditScoreAsync(int customerId)
    {
        // Mock: returns a fixed approved score for all customers
        var result = new CreditScoreResult(
            CustomerId: customerId,
            CreditScore: 1450,
            Status: "Approved"
        );
        return Task.FromResult(result);
    }
}
