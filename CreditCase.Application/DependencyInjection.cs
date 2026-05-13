using CreditCase.Application.Interfaces.Services;
using CreditCase.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCase.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Application katmanının servislerini DI container'a kaydeder.
    /// Bu katman yalnızca iş mantığı koordinasyonundan sorumludur;
    /// veritabanı veya dış servis bağımlılığı taşımaz.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ── Temel CRUD Servisleri ─────────────────────────────────────────────
        // Her servis kendi aggregate root'undan sorumludur (DDD sınır uyumu).
        // Scoped: HTTP isteği başına tek instance — repository transaction'ları ile tutarlı.
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<IPaymentService, PaymentService>();

        // ── Kredi Değerlendirme Servisi ───────────────────────────────────────
        // Risk analizi, vade farkı hesabı ve onay/red kararını koordine eder.
        // Infrastructure'daki engine'leri (IRiskAnalysisService, IInterestCalculationService vb.)
        // tüketir; doğrudan repository bağımlılığı yoktur.
        services.AddScoped<ILoanEvaluationService, LoanEvaluationService>();

        // ── FluentValidation ──────────────────────────────────────────────────
        // Assembly taraması ile bu projedeki tüm AbstractValidator<T> sınıfları
        // otomatik olarak kaydedilir — her yeni validator için manuel kayıt gerekmez.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
