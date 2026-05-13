using CreditCase.Application.Interfaces.External;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Interfaces;
using CreditCase.Infrastructure.Persistence;
using CreditCase.Infrastructure.Persistence.Repositories;
using CreditCase.Infrastructure.Services;
using CreditCase.Infrastructure.Services.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCase.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure katmanının tüm bağımlılıklarını DI container'a kaydeder.
    /// Veritabanı, dış servisler, rule engine ve strateji implementasyonları burada tanımlanır.
    /// Application katmanı yalnızca interface'leri görür; concrete sınıflardan habersizdir.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Veritabanı ────────────────────────────────────────────────────────
        // SQL Server bağlantısı appsettings'den okunur.
        // DbContext Scoped'dur: EF Core Unit of Work sınırı HTTP isteğiyle örtüşür.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // ── Repository Katmanı ────────────────────────────────────────────────
        // Her repository yalnızca kendi aggregate root'unu yönetir.
        // Application servisleri bu interface'leri tüketir; EF Core'a doğrudan bağımlı değildir.
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IInstallmentRepository, InstallmentRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ILoanEvaluationRepository, LoanEvaluationRepository>();

        // ── Dış Servis Entegrasyonları ────────────────────────────────────────
        // Üretim ortamında gerçek servis implementasyonu bu satır değiştirilerek takılır;
        // Application veya Domain katmanına dokunulmaz (DIP uyumu).
        services.AddScoped<ICreditScoreService, MockCreditScoreService>();

        // ── Risk Analizi — Kural Motoru (Rule Engine) ─────────────────────────
        // Her kural IRiskAnalysisRule'u implement eder ve bağımsız olarak kaydedilir.
        // RiskAnalysisEngine, IEnumerable<IRiskAnalysisRule> inject alır; tüm kuralları
        // sırayla çalıştırır. Yeni kural eklemek için mevcut kod değiştirilmez — sadece
        // yeni sınıf yazılır ve buraya bir satır eklenir (OCP uyumu).
        services.AddScoped<IRiskAnalysisRule, CreditScoreRule>();       // Kredi skoru ağırlığı
        services.AddScoped<IRiskAnalysisRule, DebtToIncomeRule>();      // Borç/gelir oranı kontrolü
        services.AddScoped<IRiskAnalysisRule, AgeRule>();               // Yaş sınır kontrolü (21-70)
        services.AddScoped<IRiskAnalysisRule, ProfessionStabilityRule>(); // Meslek stabilite puanı
        services.AddScoped<IRiskAnalysisRule, EmploymentStatusRule>();  // İstihdam durumu kontrolü

        // ── Değerlendirme Motorları ───────────────────────────────────────────
        // LoanEvaluationService bu üç engine'i koordineli şekilde kullanır (SRP uyumu).
        services.AddScoped<IRiskAnalysisService, RiskAnalysisEngine>();             // Risk seviyesi belirleme
        services.AddScoped<IInterestCalculationService, InterestCalculationEngine>(); // Vade farkı hesaplama
        services.AddScoped<IMaximumLoanCalculatorService, MaximumLoanCalculator>(); // Maksimum kredi limiti

        // ── Taksit Planı Stratejileri (Strategy Pattern) ─────────────────────
        // Kredi türüne göre farklı taksit hesaplama algoritmaları uygulanır.
        // InstallmentGenerator, IEnumerable<IInstallmentPlanStrategy> inject alarak
        // doğru stratejiyi çalışma zamanında seçer.
        services.AddScoped<IInstallmentPlanStrategy, StandardInstallmentStrategy>(); // Sabit eşit taksit
        services.AddScoped<IInstallmentPlanStrategy, BalloonPaymentStrategy>();      // Son taksit balon ödeme

        return services;
    }
}
