using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using CreditCase.Domain.Interfaces;

namespace CreditCase.Infrastructure.Services;

/// <summary>
///   Risk Hesaplama Algoritması:
///   TotalScore = Σ (rule.Evaluate() × rule.Weight)
///   Skor [0-100] → kategori eşiği: ≥75=Low, ≥55=Medium, ≥35=High, <35=VeryHigh
///
/// Kurallar DI ile inject edildiğinden yeni kural eklemek bu sınıfa dokunmayı gerektirmez.
/// </summary>
public class RiskAnalysisEngine : IRiskAnalysisService
{
    private readonly IEnumerable<IRiskAnalysisRule> _rules;

    public RiskAnalysisEngine(IEnumerable<IRiskAnalysisRule> rules)
    {
        _rules = rules;
    }

    public RiskAnalysisResult Analyze(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore)
    {
        decimal totalScore = _rules.Sum(r => r.Evaluate(customer, requestedAmount, requestedTerm, creditScore) * r.Weight);

        RiskCategory category = totalScore switch
        {
            >= 75 => RiskCategory.Low,
            >= 55 => RiskCategory.Medium,
            >= 35 => RiskCategory.High,
            _     => RiskCategory.VeryHigh
        };

        return new RiskAnalysisResult(category, Math.Round(totalScore, 2));
    }
}
