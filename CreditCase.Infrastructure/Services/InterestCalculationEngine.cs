using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// CLAUDE.md §6A Faiz Oranı Formülü:
///   Son Faiz = Temel (%5) + Risk Primi + Vade Primi + Tutar Primi - Meslek Bonusu
///
///   Risk Primi   : Low=0%, Medium=+5%, High=+12%
///   Vade Primi   : ≤6ay=0%, 7-12=+3%, 13-24=+7%, 25-36=+12%, 37-60=+18%, 61-84=+25%, 85+=+35%
///   Tutar Primi  : ≤2x gelir=0%, 2-3x=+1%, >3x=+2%
///   Meslek Bonusu: Government=-1%
/// </summary>
public class InterestCalculationEngine : IInterestCalculationService
{
    private const decimal BaseRate = 5.0m;

    public decimal Calculate(RiskCategory risk, int termMonths, decimal requestedAmount, Customer customer)
    {
        decimal riskPremium = risk switch
        {
            RiskCategory.Low    => 0m,
            RiskCategory.Medium => 5m,
            RiskCategory.High   => 12m,
            _                   => 0m  // VeryHigh: reddedilir, bu noktaya ulaşmamalı
        };

        // Vade uzadıkça belirsizlik artar → prim kademeli olarak yükselir.
        decimal termPremium = termMonths switch
        {
            <= 6  => 0m,
            <= 12 => 3m,
            <= 24 => 7m,
            <= 36 => 12m,
            <= 60 => 18m,
            <= 84 => 25m,
            _     => 35m   // 85-120 ay
        };

        decimal incomeMultiple = customer.MonthlyIncome > 0
            ? requestedAmount / customer.MonthlyIncome
            : 0;

        decimal amountPremium = incomeMultiple switch
        {
            <= 2m => 0m,
            <= 3m => 1m,
            _     => 2m
        };

        decimal professionBonus = customer.ProfessionCategory == ProfessionCategory.Government ? 1m : 0m;

        return Math.Round(BaseRate + riskPremium + termPremium + amountPremium - professionBonus, 2);
    }
}
