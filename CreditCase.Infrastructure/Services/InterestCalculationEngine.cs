using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// Aylık bazlı dinamik vade oranı hesaplama:
///   1. Kredi türü × ScoreCategory → aylık temel oran (12 ay referans)
///   2. Vade süresine göre çarpan uygulanır
///   3. Meslek bonusu/penaltısı eklenir
///
/// Sonuç AYLIK yüzde olarak döner (örn: 3.91 → aylık %3.91).
/// Amortisasyon formülünde r = rateAmount / 100 (doğrudan aylık oran).
///
/// Referans oranlar (Dengeli kategorisi, 12 ay): Personal=3.91, Vehicle=3.30, Education=2.85
/// </summary>
public class InterestCalculationEngine : IInterestCalculationService
{
    // Temel Aylık Vade Oranları — 12 ay referans, Türkiye bankacılık standartları.
    // Kullanıcı referans değerleri: PersonalLoan=3.91, VehicleLoan=3.30, EducationLoan=2.85 (Dengeli).
    private static readonly Dictionary<LoanType, Dictionary<ScoreCategory, decimal>> BaseRates = new()
    {
        [LoanType.Personal] = new()
        {
            [ScoreCategory.Kritik]       = 5.80m,
            [ScoreCategory.GelisimeAcik] = 4.80m,
            [ScoreCategory.Dengeli]      = 3.91m,
            [ScoreCategory.Guvenli]      = 3.05m,
            [ScoreCategory.Prestijli]    = 2.20m,
        },
        [LoanType.Vehicle] = new()
        {
            [ScoreCategory.Kritik]       = 5.10m,
            [ScoreCategory.GelisimeAcik] = 4.15m,
            [ScoreCategory.Dengeli]      = 3.30m,
            [ScoreCategory.Guvenli]      = 2.55m,
            [ScoreCategory.Prestijli]    = 1.80m,
        },
        [LoanType.Education] = new()
        {
            [ScoreCategory.Kritik]       = 4.50m,
            [ScoreCategory.GelisimeAcik] = 3.60m,
            [ScoreCategory.Dengeli]      = 2.85m,
            [ScoreCategory.Guvenli]      = 2.20m,
            [ScoreCategory.Prestijli]    = 1.55m,
        },
    };

    // Vade Faktörü — aylık bazda küçük artış/azalış.
    private static decimal TermFactor(int termMonths) => termMonths switch
    {
        <= 6  => -0.08m,
        <= 12 =>  0.00m,
        <= 18 => +0.03m,
        <= 24 => +0.06m,
        <= 36 => +0.11m,
        <= 48 => +0.17m,
        <= 60 => +0.23m,
        _     => +0.30m   // 61-72 ay
    };

    // Meslek Bonusu/Penaltısı — aylık yüzde puan.
    private static decimal ProfessionBonus(Customer customer)
    {
        decimal bonus = customer.ProfessionCategory switch
        {
            ProfessionCategory.Government   => -0.20m,
            ProfessionCategory.Healthcare   => -0.15m,
            ProfessionCategory.Technology   => -0.15m,
            ProfessionCategory.Education    => -0.10m,
            ProfessionCategory.Finance      => -0.08m,
            ProfessionCategory.Commerce     => +0.15m,
            ProfessionCategory.Construction => +0.15m,
            ProfessionCategory.Seasonal     => +0.25m,
            _                               => 0m
        };

        // Serbest çalışanlar ek risk taşır.
        if (customer.EmploymentStatus == EmploymentStatus.Freelance)
            bonus = Math.Max(bonus, 0.25m);

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
