using CreditCase.Domain.Entities;
using CreditCase.Domain.Interfaces;

namespace CreditCase.Infrastructure.Services.Rules;

/// <summary>
/// Kredi skoru bileşeni. CLAUDE.md: (KrediBorsaSkorPuanı × 0.30)
/// Skor: 0-1000 → normalize edilip 0-100 aralığına çekilir.
/// </summary>
public class CreditScoreRule : IRiskAnalysisRule
{
    public decimal Weight => 0.30m;

    public decimal Evaluate(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore)
        => Math.Clamp(creditScore / 10m, 0, 100);
}
