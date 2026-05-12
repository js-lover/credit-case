using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// claude.md §6A Dinamik Vade Oranı Hesaplama:
///   1. Kredi türü × kredi skoru kategorisi → temel vade oranı (12 ay referans)
///   2. Vade süresine göre faktör uygulanır
///   3. Meslek bonusu/penaltısı eklenir
///
/// Sonuç ratio formatındadır (örn: 3.25, 4.48); yüzde değildir (UI'de "%" kullanılmaz).
/// </summary>
public class InterestCalculationEngine : IInterestCalculationService
{
    // Temel Vade Oranları — 12 ay vade için referans (Kredi Türü × ScoreCategory).
    // claude.md §6A Kredi Türüne Göre Temel Vade Oranı tablosu.
    private static readonly Dictionary<LoanType, Dictionary<ScoreCategory, decimal>> BaseRates = new()
    {
        [LoanType.Personal] = new()
        {
            [ScoreCategory.Kritik]       = 6.8m,
            [ScoreCategory.GelisimeAcik] = 5.2m,
            [ScoreCategory.Dengeli]      = 4.0m,
            [ScoreCategory.Guvenli]      = 3.0m,
            [ScoreCategory.Prestijli]    = 2.0m,
        },
        [LoanType.Vehicle] = new()
        {
            [ScoreCategory.Kritik]       = 5.8m,
            [ScoreCategory.GelisimeAcik] = 4.2m,
            [ScoreCategory.Dengeli]      = 3.0m,
            [ScoreCategory.Guvenli]      = 2.0m,
            [ScoreCategory.Prestijli]    = 1.2m,
        },
        [LoanType.Education] = new()
        {
            [ScoreCategory.Kritik]       = 5.2m,
            [ScoreCategory.GelisimeAcik] = 3.8m,
            [ScoreCategory.Dengeli]      = 2.7m,
            [ScoreCategory.Guvenli]      = 1.7m,
            [ScoreCategory.Prestijli]    = 0.9m,
        },
    };

    // Vade Faktörü Tablosu — claude.md §6A.
    private static decimal TermFactor(int termMonths) => termMonths switch
    {
        <= 6  => -0.25m,
        <= 12 =>  0.00m,
        <= 18 => +0.08m,
        <= 24 => +0.15m,
        <= 36 => +0.28m,
        <= 48 => +0.42m,
        <= 60 => +0.58m,
        _     => +0.75m   // 61-72 ay
    };

    // Meslek Bonusu/Penaltısı — claude.md §6A.
    private static decimal ProfessionBonus(Customer customer)
    {
        decimal bonus = customer.ProfessionCategory switch
        {
            ProfessionCategory.Government   => -0.3m,
            ProfessionCategory.Healthcare   => -0.2m,
            ProfessionCategory.Technology   => -0.2m,
            ProfessionCategory.Education    => -0.15m,
            ProfessionCategory.Finance      => -0.1m,
            ProfessionCategory.Commerce     => +0.2m,
            ProfessionCategory.Construction => +0.2m,
            ProfessionCategory.Seasonal     => +0.3m,
            _                               => 0m
        };

        // Serbest çalışanlar ek risk taşır.
        if (customer.EmploymentStatus == EmploymentStatus.Freelance)
            bonus = Math.Max(bonus, 0.3m);

        return bonus;
    }

    public decimal Calculate(int creditScore, LoanType loanType, int termMonths, Customer customer)
    {
        var scoreCategory = ScoreCategoryHelper.FromScore(creditScore);

        if (!BaseRates.TryGetValue(loanType, out var categoryRates))
            categoryRates = BaseRates[LoanType.Personal];

        decimal baseRate   = categoryRates[scoreCategory];
        decimal termFactor = TermFactor(termMonths);
        decimal adjusted   = baseRate * (1m + termFactor);
        decimal final      = adjusted + ProfessionBonus(customer);

        return Math.Round(Math.Max(0.1m, final), 2);
    }
}
