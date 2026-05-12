using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Application.Interfaces.Services;

public record MaximumLoanResult(decimal MaximumAmount, int MaximumTerm);

public interface IMaximumLoanCalculatorService
{
    /// <summary>
    /// claude.md §6A: Gelir × kategori katsayısı, borç kapasitesi ve banka limiti ile
    /// müşterinin alabileceği maksimum tutarı hesaplar.
    /// </summary>
    MaximumLoanResult Calculate(Customer customer, ScoreCategory scoreCategory, int requestedTerm);
}
