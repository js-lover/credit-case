using CreditCase.Application.Interfaces.Services;
using CreditCase.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ILoanEvaluationService, LoanEvaluationService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
