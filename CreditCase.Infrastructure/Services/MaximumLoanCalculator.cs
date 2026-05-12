using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// claude.md §6A Maksimum Kredi Tutarı:
///   Kategori katsayısı: Kritik=0x (red), GelisimeAcik=3x, Dengeli=10x, Guvenli=15x, Prestijli=20x
///   MaxTutar = Min(Gelir × Katsayı, BorçKapasitesi × Vade, BankaSınırı)
/// </summary>
public class MaximumLoanCalculator : IMaximumLoanCalculatorService
{
    private const decimal BankLimit = 1_000_000m;

    public MaximumLoanResult Calculate(Customer customer, ScoreCategory scoreCategory, int requestedTerm)
    {
        decimal multiplier = ScoreCategoryHelper.IncomeMultiplier(scoreCategory);
        int maxTerm = ScoreCategoryHelper.MaxTermMonths(scoreCategory);

        if (multiplier == 0)
            return new MaximumLoanResult(0, 0);

        // Mevcut aktif kredilerin ortalama aylık taksit yükü.
        decimal existingMonthlyDebt = customer.Loans
            .Where(l => l.Status == LoanStatus.Active && l.Installments.Any())
            .Sum(l => l.Installments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .Select(i => i.Amount)
                .DefaultIfEmpty(0)
                .Average());

        decimal debtCapacity = customer.MonthlyIncome * 0.70m - existingMonthlyDebt;
        if (debtCapacity <= 0)
            return new MaximumLoanResult(0, maxTerm);

        int effectiveTerm = Math.Min(requestedTerm, maxTerm);
        decimal incomeBasedMax  = customer.MonthlyIncome * multiplier;
        decimal capacityBasedMax = debtCapacity * effectiveTerm;

        decimal maximumAmount = Math.Min(Math.Min(incomeBasedMax, capacityBasedMax), BankLimit);
        maximumAmount = Math.Max(0, Math.Round(maximumAmount, 2));

        return new MaximumLoanResult(maximumAmount, maxTerm);
    }
}
