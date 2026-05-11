using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using CreditCase.Domain.Interfaces;

namespace CreditCase.Infrastructure.Services.Rules;

/// <summary>
/// Meslek stabilite bileşeni. CLAUDE.md: (MeslekStabilitesiPuanı × 0.20)
/// Kamu, Sağlık, Finans, Eğitim sektörleri en stabil; mevsimlik iş ve belirsiz istihdam risklidir.
/// </summary>
public class ProfessionStabilityRule : IRiskAnalysisRule
{
    public decimal Weight => 0.20m;

    public decimal Evaluate(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore)
        => customer.ProfessionCategory switch
        {
            ProfessionCategory.Government  => 100,
            ProfessionCategory.Healthcare  => 100,
            ProfessionCategory.Finance     => 100,
            ProfessionCategory.Education   => 100,
            ProfessionCategory.Technology  => 90,
            ProfessionCategory.Commerce    => 60,
            ProfessionCategory.Services    => 60,
            ProfessionCategory.Construction => 40,
            ProfessionCategory.Seasonal    => 20,
            _                              => 10
        };
}
