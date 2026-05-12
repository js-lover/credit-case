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
    private const decimal RegularPaymentRatio = 0.60m;
    // KKDF+BSMV (%20) brüt oran nedeniyle balon tutar net anaparanın %50'sini
    // sistematik olarak aşar; %90 sınırı KKDF/BSMV dahil gerçekçi üst limiti ifade eder.
    private const decimal MaxBalloonRatio = 0.90m;
    private const int    MaxBalloonTerm   = 36;

    public bool SupportsBalloon => true;

    public List<Installment> Generate(decimal principalAmount, decimal rateAmount, int term, DateTime startDate)
    {
        if (term > MaxBalloonTerm)
            throw new BusinessRuleException(
                $"Balon ödeme planı en fazla {MaxBalloonTerm} ay vade için oluşturulabilir.");

        decimal normalMonthly = StandardInstallmentStrategy.ComputeMonthly(principalAmount, rateAmount, term);
        decimal totalAmount = normalMonthly * term;

        decimal regularAmount = Math.Round(normalMonthly * RegularPaymentRatio, 2);
        decimal balloonAmount = Math.Round(totalAmount - regularAmount * (term - 1), 2);

        if (balloonAmount > principalAmount * MaxBalloonRatio)
            throw new BusinessRuleException(
                $"Balon taksit tutarı, anaparanın {MaxBalloonRatio:P0}'ini aşmaktadır. " +
                "Vadeyi kısaltın veya kredi tutarını düşürün.");

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
