using CreditCase.Domain.Entities;

namespace CreditCase.Application.Interfaces.Services;

public interface IInstallmentPlanStrategy
{
    bool SupportsBalloon { get; }
    List<Installment> Generate(decimal principalAmount, decimal interestRate, int term, DateTime startDate);
}
