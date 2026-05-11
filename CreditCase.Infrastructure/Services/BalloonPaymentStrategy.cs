using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// Balon ödeme planı üretir: ilk n-1 taksit düşük tutarda, son taksit (balon)
/// kalan borcun tamamını içerir. Balon tutar anaparanın %50'sini aşamaz.
/// </summary>
public class BalloonPaymentStrategy : IInstallmentPlanStrategy
{
    private const decimal RegularPaymentRatio = 0.60m;   // Normal aylık tutarın %60'ı
    private const decimal MaxBalloonRatio = 0.50m;        // Anapara ile karşılaştırma limiti

    public bool SupportsBalloon => true;

    public List<Installment> Generate(decimal principalAmount, decimal interestRate, int term, DateTime startDate)
    {
        decimal termYears = term / 12m;
        decimal totalAmount = principalAmount * (1 + interestRate / 100 * termYears);
        decimal normalMonthly = totalAmount / term;

        decimal regularAmount = Math.Round(normalMonthly * RegularPaymentRatio, 2);
        decimal balloonAmount = Math.Round(totalAmount - regularAmount * (term - 1), 2);

        if (balloonAmount > principalAmount * MaxBalloonRatio)
            throw new BusinessRuleException(
                $"Balon taksit tutarı, anaparanın {MaxBalloonRatio:P0}'ini aşmaktadır. " +
                "Vadeyi uzatın veya kredi tutarını düşürün.");

        var installments = Enumerable.Range(1, term - 1)
            .Select(i => new Installment
            {
                InstallmentNumber = i,
                Amount = regularAmount,
                DueDate = startDate.AddMonths(i),
                Status = InstallmentStatus.Unpaid,
                IsBalloon = false
            })
            .ToList();

        installments.Add(new Installment
        {
            InstallmentNumber = term,
            Amount = balloonAmount,
            DueDate = startDate.AddMonths(term),
            Status = InstallmentStatus.Unpaid,
            IsBalloon = true
        });

        return installments;
    }
}
