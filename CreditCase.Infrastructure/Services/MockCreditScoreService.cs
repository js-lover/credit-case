using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// Dış kredi skoru servisinin mock implementasyonu. claude.md §12.
///
/// Skor 4 bileşenden hesaplanır (toplam baz maks. 1700):
///   Yaş           → maks. 400 puan
///   Aylık Gelir   → maks. 550 puan
///   İstihdam      → maks. 400 puan
///   Meslek        → maks. 350 puan
///
/// Ödeme geçmişi bonusu (Customer.CreditScoreBonus, -200 / +200) baz skora eklenir.
/// Nihai skor [0, 1900] aralığında kısıtlanır; claude.md §6A standartlarıyla örtüşür.
/// </summary>
public class MockCreditScoreService : ICreditScoreService
{
    private readonly ICustomerRepository _customerRepository;

    public MockCreditScoreService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CreditScoreResult> GetCreditScoreAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);

        int finalScore;
        if (customer is null)
        {
            finalScore = 800;  // Profil bulunamazsa Dengeli alt sınırı
        }
        else
        {
            int ageScore        = ScoreAge(customer.DateOfBirth);
            int incomeScore     = ScoreIncome(customer.MonthlyIncome);
            int employmentScore = ScoreEmployment(customer.EmploymentStatus);
            int professionScore = ScoreProfession(customer.ProfessionCategory);

            int baseScore = ageScore + incomeScore + employmentScore + professionScore;
            finalScore = Math.Clamp(baseScore + customer.CreditScoreBonus, 0, 1900);
        }

        var scoreCategory = ScoreCategoryHelper.FromScore(finalScore);
        string indicator = scoreCategory switch
        {
            ScoreCategory.Prestijli    => "Low",
            ScoreCategory.Guvenli      => "Low",
            ScoreCategory.Dengeli      => "Medium",
            ScoreCategory.GelisimeAcik => "High",
            _                          => "VeryHigh"
        };
        decimal defaultProbability = scoreCategory switch
        {
            ScoreCategory.Prestijli    => 0.02m,
            ScoreCategory.Guvenli      => 0.05m,
            ScoreCategory.Dengeli      => 0.12m,
            ScoreCategory.GelisimeAcik => 0.25m,
            _                          => 0.45m
        };

        IReadOnlyList<NegativeRecord> negativeRecords = finalScore < 1150
            ? [new NegativeRecord("Payment Late", DateTime.UtcNow.AddMonths(-6), 1_200m)]
            : [];

        return new CreditScoreResult(
            CustomerId: customerId,
            CreditScore: finalScore,
            RiskIndicator: indicator,
            NegativeRecords: negativeRecords,
            DefaultProbability: defaultProbability,
            QueryDate: DateTime.UtcNow
        );
    }

    // ── Yaş puanı (maks. 400) ────────────────────────────────────────────────────
    private static int ScoreAge(DateTime dateOfBirth)
    {
        int age = (int)((DateTime.UtcNow - dateOfBirth).TotalDays / 365.25);
        return age switch
        {
            < 21  => 80,
            < 26  => 200,
            < 36  => 320,
            < 51  => 400,   // pik — 36-50
            < 61  => 350,
            < 66  => 240,
            _     => 110
        };
    }

    // ── Gelir puanı (maks. 550) ──────────────────────────────────────────────────
    private static int ScoreIncome(decimal monthlyIncome) => monthlyIncome switch
    {
        < 3_000m    => 60,
        < 6_000m    => 170,
        < 10_000m   => 290,
        < 20_000m   => 410,
        < 50_000m   => 495,
        _           => 550
    };

    // ── İstihdam durumu puanı (maks. 400) ────────────────────────────────────────
    private static int ScoreEmployment(EmploymentStatus status) => status switch
    {
        EmploymentStatus.Active     => 400,
        EmploymentStatus.Retired    => 340,
        EmploymentStatus.Freelance  => 260,
        EmploymentStatus.PartTime   => 200,
        EmploymentStatus.Unemployed => 40,
        _                           => 200
    };

    // ── Meslek kategorisi puanı (maks. 350) ──────────────────────────────────────
    private static int ScoreProfession(ProfessionCategory category) => category switch
    {
        ProfessionCategory.Government   => 350,
        ProfessionCategory.Healthcare   => 315,
        ProfessionCategory.Finance      => 295,
        ProfessionCategory.Education    => 280,
        ProfessionCategory.Technology   => 280,
        ProfessionCategory.Commerce     => 220,
        ProfessionCategory.Services     => 195,
        ProfessionCategory.Other        => 175,
        ProfessionCategory.Construction => 150,
        ProfessionCategory.Seasonal     => 90,
        _                               => 175
    };
}
