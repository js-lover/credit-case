namespace CreditCase.Application.Interfaces.External;

public record NegativeRecord(string RecordType, DateTime RecordDate, decimal Amount);

/// <summary>
/// Dış kredi skoru servis yanıtı. CLAUDE.md §6A Mock Dış Servis Entegrasyonu.
/// </summary>
public record CreditScoreResult(
    int CustomerId,
    int CreditScore,             // 0-1000
    string RiskIndicator,        // Low, Medium, High
    IReadOnlyList<NegativeRecord> NegativeRecords,
    decimal DefaultProbability,  // 0.0-1.0
    DateTime QueryDate
);

public interface ICreditScoreService
{
    Task<CreditScoreResult> GetCreditScoreAsync(int customerId);
}
