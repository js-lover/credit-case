using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Application.Interfaces.Services;

public record RiskAnalysisResult(RiskCategory Category, decimal TotalScore);

public interface IRiskAnalysisService
{
    RiskAnalysisResult Analyze(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore);
}
