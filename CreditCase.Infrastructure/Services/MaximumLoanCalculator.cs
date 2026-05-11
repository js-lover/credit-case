using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Infrastructure.Services;

/// <summary>
/// CLAUDE.md §6A Maksimum Kredi Tutarı:
///   BorçKapasitesi = (AylıkGelir × 0.70) - MevcutAylıkBorçÖdemesi
///   KullanılabilirTutar = BorçKapasitesi × VadeAy
///   RiskKatsayısı: Low=5.0x, Medium=3.5x, High=2.0x
///   MaxTutar = Min(Gelir × RiskKatsayısı, KullanılabilirTutar, BankaSınırı=1_000_000)
/// </summary>
public class MaximumLoanCalculator : IMaximumLoanCalculatorService
{
    private const decimal BankLimit = 1_000_000m;
    private const int MaxAllowedTerm = 120;

    public MaximumLoanResult Calculate(Customer customer, RiskCategory risk, int requestedTerm)
    {
        decimal riskCoefficient = risk switch
        {
            RiskCategory.Low    => 5.0m,
            RiskCategory.Medium => 3.5m,
            RiskCategory.High   => 2.0m,
            _                   => 0m
        };

        if (riskCoefficient == 0)
            return new MaximumLoanResult(0, 0);

        // Mevcut aktif kredilerin ortalama aylık taksit yükü.
        decimal existingMonthlyDebt = customer.Loans
            .Where(l => l.Status == LoanStatus.Active && l.Installments.Any())
            .Sum(l => l.Installments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .Select(i => i.Amount)
                .DefaultIfEmpty(0)
                .Average());

        decimal debtCapacity = (customer.MonthlyIncome * 0.70m) - existingMonthlyDebt;
        if (debtCapacity <= 0)
            return new MaximumLoanResult(0, 0);

        decimal incomeBasedMax = customer.MonthlyIncome * riskCoefficient;
        decimal capacityBasedMax = debtCapacity * requestedTerm;

        decimal maximumAmount = Math.Min(Math.Min(incomeBasedMax, capacityBasedMax), BankLimit);
        maximumAmount = Math.Max(0, Math.Round(maximumAmount, 2));

        return new MaximumLoanResult(maximumAmount, MaxAllowedTerm);
    }
}
