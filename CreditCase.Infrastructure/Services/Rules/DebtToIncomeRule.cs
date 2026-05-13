using CreditCase.Domain.Entities;
using CreditCase.Domain.Interfaces;

namespace CreditCase.Infrastructure.Services.Rules;

/// <summary>
/// Borç/Gelir oranı bileşeni. CLAUDE.md: (BorcGelirOranıPuanı × 0.25)
/// Mevcut kredilerin aylık taksit yükü gelire oranlanır.
/// </summary>
public class DebtToIncomeRule : IRiskAnalysisRule
{
    public decimal Weight => 0.25m;

    public decimal Evaluate(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore)
    {
        if (customer.MonthlyIncome <= 0) return 0;

        // Mevcut aktif kredilerin kalan borcunu aylık yüke dönüştür (kaba tahmin).
        decimal existingMonthlyDebt = customer.Loans
            .Where(l => l.Status == Domain.Enums.LoanStatus.Active && l.Installments.Any())
            .Sum(l => l.Installments
                .Where(i => i.Status != Domain.Enums.InstallmentStatus.Paid)
                .Select(i => i.Amount)
                .DefaultIfEmpty(0)
                .Average());

        // İstenen kredinin tahmini aylık ödemesi (düz vade farkı tahmini).
        decimal estimatedNewMonthly = requestedTerm > 0 ? requestedAmount / requestedTerm : 0;

        decimal ratio = (existingMonthlyDebt + estimatedNewMonthly) / customer.MonthlyIncome;

        return ratio switch
        {
            <= 0.30m => 100,
            <= 0.50m => 70,
            <= 0.70m => 40,
            _        => 10
        };
    }
}
