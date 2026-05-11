using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// Dış kredi skoru servisinin mock implementasyonu. CLAUDE.md §12.
///
/// Skor 4 bileşenden hesaplanır (toplam baz maks. 800):
///   Yaş           → maks. 200 puan
///   Aylık Gelir   → maks. 250 puan
///   İstihdam      → maks. 200 puan
///   Meslek        → maks. 150 puan
///
/// Ödeme geçmişi bonusu (Customer.CreditScoreBonus, -200 / +200) baz skora eklenir.
/// Nihai skor [0, 1000] aralığında kısıtlanır.
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
            // Profil bulunamazsa minimum geçerli skor.
            finalScore = 400;
        }
        else
        {
            int ageScore        = ScoreAge(customer.DateOfBirth);
            int incomeScore     = ScoreIncome(customer.MonthlyIncome);
            int employmentScore = ScoreEmployment(customer.EmploymentStatus);
            int professionScore = ScoreProfession(customer.ProfessionCategory);

            int baseScore = ageScore + incomeScore + employmentScore + professionScore;
            finalScore = Math.Clamp(baseScore + customer.CreditScoreBonus, 0, 1000);
        }

        string indicator = finalScore >= 750 ? "Low" : finalScore >= 600 ? "Medium" : "High";
        decimal defaultProbability = finalScore >= 750 ? 0.05m : finalScore >= 600 ? 0.15m : 0.35m;

        IReadOnlyList<NegativeRecord> negativeRecords = finalScore < 600
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

    // ── Yaş puanı (maks. 200) ────────────────────────────────────────────────────
    // 36-50 yaş bandı en yüksek kredi güvenilirliğini temsil eder.
    // Çok genç ve çok yaşlı müşteriler daha düşük puan alır.
    private static int ScoreAge(DateTime dateOfBirth)
    {
        int age = (int)((DateTime.UtcNow - dateOfBirth).TotalDays / 365.25);
        return age switch
        {
            < 21      => 40,
            < 26      => 100,
            < 36      => 160,
            < 51      => 200,   // pik — 36-50
            < 61      => 175,
            < 66      => 120,
            _         => 55
        };
    }

    // ── Gelir puanı (maks. 250) ──────────────────────────────────────────────────
    // Türk bankacılığı referans gelir bantları.
    private static int ScoreIncome(decimal monthlyIncome) => monthlyIncome switch
    {
        < 3_000m    => 30,
        < 6_000m    => 85,
        < 10_000m   => 145,
        < 20_000m   => 205,
        < 50_000m   => 240,
        _           => 250
    };

    // ── İstihdam durumu puanı (maks. 200) ────────────────────────────────────────
    private static int ScoreEmployment(EmploymentStatus status) => status switch
    {
        EmploymentStatus.Active     => 200,   // sabit işçi — en güvenli
        EmploymentStatus.Retired    => 170,   // düzenli emekli maaşı
        EmploymentStatus.Freelance  => 130,   // değişken gelir
        EmploymentStatus.PartTime   => 100,   // kısmi çalışma
        EmploymentStatus.Unemployed => 20,    // aktif gelir yok
        _                           => 100
    };

    // ── Meslek kategorisi puanı (maks. 150) ──────────────────────────────────────
    // Kamu ve sağlık sektörü geleneksel olarak en stabil kabul edilir.
    private static int ScoreProfession(ProfessionCategory category) => category switch
    {
        ProfessionCategory.Government   => 150,
        ProfessionCategory.Healthcare   => 140,
        ProfessionCategory.Finance      => 135,
        ProfessionCategory.Education    => 130,
        ProfessionCategory.Technology   => 130,
        ProfessionCategory.Commerce     => 100,
        ProfessionCategory.Services     => 90,
        ProfessionCategory.Other        => 80,
        ProfessionCategory.Construction => 70,
        ProfessionCategory.Seasonal     => 45,
        _                               => 80
    };
}
