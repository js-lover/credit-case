using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Application.Interfaces.Services;

public interface IInterestCalculationService
{
    /// <summary>
    /// claude.md §6A Dinamik Vade Oranı Hesaplama:
    ///   Base = BaseRates[loanType][scoreCategory]
    ///   Son Vade Oranı = Base × (1 + TermFactor) ± ProfessionBonus
    /// Sonuç yüzde değil ratio formatındadır (örn: 3.25, 4.48).
    /// </summary>
    decimal Calculate(int creditScore, LoanType loanType, int termMonths, Customer customer);
}
