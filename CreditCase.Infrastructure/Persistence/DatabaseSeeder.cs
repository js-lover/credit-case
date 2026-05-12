using CreditCase.Application.DTOs.Customers;
using CreditCase.Application.DTOs.Loans;
using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCase.Infrastructure.Persistence;

/// <summary>
/// Geliştirme ortamı için demo veri tohumcusu.
/// appsettings.Development.json içinde "SeedDatabase": true ise çalışır.
///
/// Müşteri profili çeşitliliği:
///   Güvenli   (1470-1719) → Mustafa Koç, Selin Yıldız, Dr. Fatma Öz
///   Dengeli   (1150-1469) → Emre Çelik, Ayşe Kara
///   Gelişime Açık (970-1149) → Nur Şahin
///   Kritik    (0-969)     → Hasan Öztürk (Freelance, yüksek risk)
///
/// Kredi ve ödeme geçmişi:
///   - Mustafa Koç:  Araç 450K / 36ay → 4 taksit ödenmiş, 1 gecikmiş
///   - Selin Yıldız: İhtiyaç 180K / 24ay → 2 taksit ödenmiş
///   - Dr. Fatma Öz: Eğitim 280K / 36ay → 3 taksit ödenmiş, 2 gecikmiş
///   - Emre Çelik:   İhtiyaç 220K / 24ay → 1 taksit ödenmiş
///   - Diğerleri:    Kredi başvurusu değerlendirme kaydı mevcut (reddedildi)
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var db          = sp.GetRequiredService<AppDbContext>();
        var customerSvc = sp.GetRequiredService<ICustomerService>();
        var loanSvc     = sp.GetRequiredService<ILoanService>();
        var paymentSvc  = sp.GetRequiredService<IPaymentService>();
        var evalSvc     = sp.GetRequiredService<ILoanEvaluationService>();

        // ── 1. Mevcut tüm kayıtları temizle ──────────────────────────────────
        // EF Core Cascade delete: Customers silinince bağlı tüm tablolar temizlenir.
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Payments");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Installments");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM LoanEvaluations");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Loans");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Customers");

        // ── 2. Müşterileri oluştur ────────────────────────────────────────────

        // Güvenli (1700): age=400 + income=550 + active=400 + government=350
        var mustafa = await customerSvc.CreateAsync(new CreateCustomerRequest
        {
            FirstName          = "Mustafa",
            LastName           = "Koç",
            IdentityNumber     = "10000000001",
            Email              = "mustafa.koc@creditcase.dev",
            PhoneNumber        = "5301234501",
            DateOfBirth        = new DateTime(1978, 6, 15),
            MonthlyIncome      = 80_000m,
            ProfessionCategory = ProfessionCategory.Government,
            EmploymentStatus   = EmploymentStatus.Active
        });

        // Güvenli (1575): age=400 + income=495 + active=400 + technology=280
        var selin = await customerSvc.CreateAsync(new CreateCustomerRequest
        {
            FirstName          = "Selin",
            LastName           = "Yıldız",
            IdentityNumber     = "10000000002",
            Email              = "selin.yildiz@creditcase.dev",
            PhoneNumber        = "5301234502",
            DateOfBirth        = new DateTime(1987, 8, 20),
            MonthlyIncome      = 42_000m,
            ProfessionCategory = ProfessionCategory.Technology,
            EmploymentStatus   = EmploymentStatus.Active
        });

        // Güvenli (1665): age=400 + income=550 + active=400 + healthcare=315
        var fatma = await customerSvc.CreateAsync(new CreateCustomerRequest
        {
            FirstName          = "Fatma",
            LastName           = "Öz",
            IdentityNumber     = "10000000003",
            Email              = "fatma.oz@creditcase.dev",
            PhoneNumber        = "5301234503",
            DateOfBirth        = new DateTime(1985, 2, 15),
            MonthlyIncome      = 55_000m,
            ProfessionCategory = ProfessionCategory.Healthcare,
            EmploymentStatus   = EmploymentStatus.Active
        });

        // Dengeli (1375): age=200 (<26) + income=495 (>20K) + active=400 + technology=280
        var emre = await customerSvc.CreateAsync(new CreateCustomerRequest
        {
            FirstName          = "Emre",
            LastName           = "Çelik",
            IdentityNumber     = "10000000004",
            Email              = "emre.celik@creditcase.dev",
            PhoneNumber        = "5301234504",
            DateOfBirth        = new DateTime(2001, 11, 3),
            MonthlyIncome      = 22_000m,
            ProfessionCategory = ProfessionCategory.Technology,
            EmploymentStatus   = EmploymentStatus.Active
        });

        // Dengeli (1375): age=200 (<26) + income=495 (>20K) + active=400 + education=280
        var ayse = await customerSvc.CreateAsync(new CreateCustomerRequest
        {
            FirstName          = "Ayşe",
            LastName           = "Kara",
            IdentityNumber     = "10000000005",
            Email              = "ayse.kara@creditcase.dev",
            PhoneNumber        = "5301234505",
            DateOfBirth        = new DateTime(2000, 7, 25),
            MonthlyIncome      = 21_000m,
            ProfessionCategory = ProfessionCategory.Education,
            EmploymentStatus   = EmploymentStatus.Active
        });

        // Gelişime Açık (1005): age=320 + income=290 + parttime=200 + services=195
        var nur = await customerSvc.CreateAsync(new CreateCustomerRequest
        {
            FirstName          = "Nur",
            LastName           = "Şahin",
            IdentityNumber     = "10000000006",
            Email              = "nur.sahin@creditcase.dev",
            PhoneNumber        = "5301234506",
            DateOfBirth        = new DateTime(1993, 9, 15),
            MonthlyIncome      = 7_500m,
            ProfessionCategory = ProfessionCategory.Services,
            EmploymentStatus   = EmploymentStatus.PartTime
        });

        // Kritik/yüksek risk (1290 Dengeli ama Freelance penaltı + düşük taksit → reddedilir)
        var hasan = await customerSvc.CreateAsync(new CreateCustomerRequest
        {
            FirstName          = "Hasan",
            LastName           = "Öztürk",
            IdentityNumber     = "10000000007",
            Email              = "hasan.ozturk@creditcase.dev",
            PhoneNumber        = "5301234507",
            DateOfBirth        = new DateTime(1988, 3, 22),
            MonthlyIncome      = 14_000m,
            ProfessionCategory = ProfessionCategory.Commerce,
            EmploymentStatus   = EmploymentStatus.Freelance
        });

        // ── 3. Değerlendirmeler & Krediler ────────────────────────────────────

        // Mustafa Koç — Araç Kredisi 450K, 36 ay — ONAYLANDI
        // Başlangıç: 6 ay önce → bazı taksitler gecikmiş olacak
        var loanMustafa = await loanSvc.CreateAsync(new CreateLoanRequest
        {
            CustomerId      = mustafa.Id,
            LoanType        = LoanType.Vehicle,
            PrincipalAmount = 450_000m,
            Term            = 36,
            StartDate       = DateTime.UtcNow.AddMonths(-6)
        });

        // Selin Yıldız — İhtiyaç Kredisi 180K, 24 ay — ONAYLANDI
        // Başlangıç: 3 ay önce
        var loanSelin = await loanSvc.CreateAsync(new CreateLoanRequest
        {
            CustomerId      = selin.Id,
            LoanType        = LoanType.Personal,
            PrincipalAmount = 180_000m,
            Term            = 24,
            StartDate       = DateTime.UtcNow.AddMonths(-3)
        });

        // Dr. Fatma Öz — Eğitim Kredisi 280K, 36 ay — ONAYLANDI
        // Başlangıç: 5 ay önce
        var loanFatma = await loanSvc.CreateAsync(new CreateLoanRequest
        {
            CustomerId      = fatma.Id,
            LoanType        = LoanType.Education,
            PrincipalAmount = 280_000m,
            Term            = 36,
            StartDate       = DateTime.UtcNow.AddMonths(-5)
        });

        // Emre Çelik — İhtiyaç Kredisi 220K, 24 ay — ONAYLANDI
        // Başlangıç: 2 ay önce
        var loanEmre = await loanSvc.CreateAsync(new CreateLoanRequest
        {
            CustomerId      = emre.Id,
            LoanType        = LoanType.Personal,
            PrincipalAmount = 220_000m,
            Term            = 24,
            StartDate       = DateTime.UtcNow.AddMonths(-2)
        });

        // Ayşe, Nur, Hasan → Değerlendirme iste (reddedilir, kayıt oluşur)
        await evalSvc.EvaluateAsync(new Application.DTOs.LoanEvaluation.LoanApplicationRequest
        {
            CustomerId      = ayse.Id,
            LoanType        = LoanType.Personal,
            RequestedAmount = 200_000m,
            RequestedTerm   = 24
        });

        await evalSvc.EvaluateAsync(new Application.DTOs.LoanEvaluation.LoanApplicationRequest
        {
            CustomerId      = nur.Id,
            LoanType        = LoanType.Personal,
            RequestedAmount = 20_000m,
            RequestedTerm   = 24
        });

        await evalSvc.EvaluateAsync(new Application.DTOs.LoanEvaluation.LoanApplicationRequest
        {
            CustomerId      = hasan.Id,
            LoanType        = LoanType.Personal,
            RequestedAmount = 100_000m,
            RequestedTerm   = 24
        });

        // ── 4. Ödemeler ───────────────────────────────────────────────────────
        // Mustafa Koç: 6 taksit vadesi geldi, 4'ü ödenmiş → #5, #6 gecikmiş (overdue)
        await PayInstallmentsAsync(paymentSvc, loanMustafa, count: 4);

        // Selin Yıldız: 3 taksit vadesi geldi, 2'si ödenmiş → #3 yaklaşıyor
        await PayInstallmentsAsync(paymentSvc, loanSelin, count: 2);

        // Dr. Fatma Öz: 5 taksit vadesi geldi, 3'ü ödenmiş → #4, #5 gecikmiş
        await PayInstallmentsAsync(paymentSvc, loanFatma, count: 3);

        // Emre Çelik: 2 taksit vadesi geldi, 1'i ödenmiş
        await PayInstallmentsAsync(paymentSvc, loanEmre, count: 1);
    }

    private static async Task PayInstallmentsAsync(IPaymentService paymentSvc, LoanResponse loan, int count)
    {
        foreach (var inst in loan.Installments.OrderBy(i => i.InstallmentNumber).Take(count))
        {
            await paymentSvc.CreateAsync(new CreatePaymentRequest
            {
                InstallmentId  = inst.Id,
                PaymentAmount  = inst.Amount
            });
        }
    }
}
