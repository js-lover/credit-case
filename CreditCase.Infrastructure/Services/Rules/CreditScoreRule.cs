using CreditCase.Domain.Entities;
using CreditCase.Domain.Interfaces;

namespace CreditCase.Infrastructure.Services.Rules;

/// <summary>
/// Kredi skoru bileşeni. claude.md: (KrediBorsaSkorPuanı × 0.30)
/// Skor: 0-1900 → normalize edilip 0-100 aralığına çekilir.
/// </summary>
public class CreditScoreRule : IRiskAnalysisRule
{
    public decimal Weight => 0.30m;

    public decimal Evaluate(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore)
        => Math.Clamp(creditScore / 19m, 0, 100);
}
