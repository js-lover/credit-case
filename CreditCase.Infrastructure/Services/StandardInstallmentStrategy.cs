using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// Düz faiz (flat-rate) yöntemiyle eşit taksitli standart ödeme planı üretir.
/// </summary>
public class StandardInstallmentStrategy : IInstallmentPlanStrategy
{
    public bool SupportsBalloon => false;

    public List<Installment> Generate(decimal principalAmount, decimal interestRate, int term, DateTime startDate)
    {
        decimal termYears = term / 12m;
        decimal totalAmount = principalAmount * (1 + interestRate / 100 * termYears);
        decimal monthlyAmount = Math.Round(totalAmount / term, 2);

        return Enumerable.Range(1, term)
            .Select(i => new Installment
            {
                InstallmentNumber = i,
                Amount = monthlyAmount,
                DueDate = startDate.AddMonths(i),
                Status = InstallmentStatus.Unpaid,
                IsBalloon = false
            })
            .ToList();
    }
}
