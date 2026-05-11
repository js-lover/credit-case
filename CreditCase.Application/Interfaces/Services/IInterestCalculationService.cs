using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Application.Interfaces.Services;

public interface IInterestCalculationService
{
    /// <summary>
    /// CLAUDE.md formülü: BaseRate + RiskPremium + TermPremium + AmountPremium - ProfessionBonus
    /// </summary>
    decimal Calculate(RiskCategory risk, int termMonths, decimal requestedAmount, Customer customer);
}
