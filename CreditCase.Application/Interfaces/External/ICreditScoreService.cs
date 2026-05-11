namespace CreditCase.Application.Interfaces.External;

public record CreditScoreResult(int CustomerId, int CreditScore, string Status);

public interface ICreditScoreService
{
    Task<CreditScoreResult> GetCreditScoreAsync(int customerId);
}
