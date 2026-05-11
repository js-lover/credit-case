using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using CreditCase.Domain.Interfaces;

namespace CreditCase.Infrastructure.Services.Rules;

/// <summary>
/// İstihdam durumu bileşeni. CLAUDE.md: (IstidamDurumuPuanı × 0.10)
/// İşsiz müşteriler sıfır puan alır; bu kural VeryHigh risk kategorisini tetikleyebilir.
/// </summary>
public class EmploymentStatusRule : IRiskAnalysisRule
{
    public decimal Weight => 0.10m;

    public decimal Evaluate(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore)
        => customer.EmploymentStatus switch
        {
            EmploymentStatus.Active     => 100,
            EmploymentStatus.PartTime   => 70,
            EmploymentStatus.Retired    => 60,
            EmploymentStatus.Freelance  => 50,
            EmploymentStatus.Unemployed => 0,
            _                           => 0
        };
}
