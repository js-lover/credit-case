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
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IInstallmentRepository, InstallmentRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ILoanEvaluationRepository, LoanEvaluationRepository>();

        // External services
        services.AddScoped<ICreditScoreService, MockCreditScoreService>();

        // Risk Analysis — Rule Engine: her kural bağımsız DI kaydı (OCP uyumu)
        services.AddScoped<IRiskAnalysisRule, CreditScoreRule>();
        services.AddScoped<IRiskAnalysisRule, DebtToIncomeRule>();
        services.AddScoped<IRiskAnalysisRule, AgeRule>();
        services.AddScoped<IRiskAnalysisRule, ProfessionStabilityRule>();
        services.AddScoped<IRiskAnalysisRule, EmploymentStatusRule>();

        // Evaluation engines
        services.AddScoped<IRiskAnalysisService, RiskAnalysisEngine>();
        services.AddScoped<IInterestCalculationService, InterestCalculationEngine>();
        services.AddScoped<IMaximumLoanCalculatorService, MaximumLoanCalculator>();

        // Installment plan strategies (Strategy pattern)
        services.AddScoped<IInstallmentPlanStrategy, StandardInstallmentStrategy>();
        services.AddScoped<IInstallmentPlanStrategy, BalloonPaymentStrategy>();

        return services;
    }
}
