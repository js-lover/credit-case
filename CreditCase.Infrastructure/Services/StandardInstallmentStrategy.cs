using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// Amortisasyon yöntemiyle eşit taksitli standart ödeme planı üretir.
///   r = rateAmount / 100  (rateAmount aylık yüzdedir, doğrudan aylık oran)
///   A = P × r(1+r)^n / [(1+r)^n - 1]
/// </summary>
public class StandardInstallmentStrategy : IInstallmentPlanStrategy
{
    public bool SupportsBalloon => false;

    public List<Installment> Generate(decimal principalAmount, decimal rateAmount, int term, DateTime startDate)
    {
        decimal monthly = ComputeMonthly(principalAmount, rateAmount, term);

        return Enumerable.Range(1, term)
            .Select(i => new Installment
            {
                InstallmentNumber = i,
                Amount = monthly,
                DueDate = startDate.AddMonths(i),
                Status = InstallmentStatus.Unpaid,
                IsBalloon = false
            })
            .ToList();
    }

    internal static decimal ComputeMonthly(decimal principal, decimal rateAmount, int term)
    {
        if (term <= 0 || principal <= 0) return 0m;
        decimal r = rateAmount / 100m;
        if (r == 0m) return Math.Round(principal / term, 2);
        double factor = Math.Pow(1 + (double)r, term);
        return Math.Round(principal * (decimal)(r * (decimal)factor / ((decimal)factor - 1)), 2);
    }
}
