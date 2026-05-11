using CreditCase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCase.Infrastructure.Persistence;

/// <summary>
/// Uygulama veritabanı bağlamı. Tüm entity konfigürasyonları burada merkezi olarak yönetilir.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Installment> Installments { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<LoanEvaluationResult> LoanEvaluations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdentityNumber).IsRequired().HasMaxLength(11);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.CreditScoreBonus).HasDefaultValue(0);
            // Kredi değerlendirme profil alanları.
            entity.Property(e => e.MonthlyIncome).HasPrecision(18, 2);
            entity.Property(e => e.ProfessionCategory).HasConversion<string>();
            entity.Property(e => e.EmploymentStatus).HasConversion<string>();

            // Filtered unique indexes: soft-deleted kayıtlar (IsDeleted=1) benzersizlik
            // kısıtının dışında tutulur. Bu sayede aynı TC veya e-posta ile yeni kayıt
            // açılabilir; silinmiş müşterinin verisi DB'de kalır, yeni kayda engel olmaz.
            entity.HasIndex(e => e.IdentityNumber).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[IsDeleted] = 0");

            // Global query filter: EF Core tüm Customer sorgularına otomatik olarak
            // WHERE IsDeleted = 0 ekler. Servis ve repository koduna ayrıca filtre yazmak gerekmez.
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Finansal alanlar için decimal precision zorunludur; float/double kullanılmaz.
            entity.Property(e => e.PrincipalAmount).HasPrecision(18, 2);
            entity.Property(e => e.InterestRate).HasPrecision(5, 2);
            entity.Property(e => e.RemainingPrincipal).HasPrecision(18, 2);
            // Enum'lar DB'de okunabilir string olarak saklanır (0/1 yerine "Active"/"Closed").
            entity.Property(e => e.LoanType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Loans)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Installment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Loan)
                .WithMany(l => l.Installments)
                .HasForeignKey(e => e.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>();
            // Bir taksite en fazla bir ödeme yapılabilir (1:1 ilişki).
            entity.HasOne(e => e.Installment)
                .WithOne(i => i.Payment)
                .HasForeignKey<Payment>(e => e.InstallmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoanEvaluationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestedAmount).HasPrecision(18, 2);
            entity.Property(e => e.ApprovedAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaximumAmount).HasPrecision(18, 2);
            entity.Property(e => e.ApprovedInterestRate).HasPrecision(5, 2);
            entity.Property(e => e.DebtToIncomeRatio).HasPrecision(5, 4);
            entity.Property(e => e.MonthlyInstallmentEstimate).HasPrecision(18, 2);
            entity.Property(e => e.RiskLevel).HasConversion<string>();
            entity.Property(e => e.RequestedLoanType).HasConversion<string>();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.LoanEvaluations)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
