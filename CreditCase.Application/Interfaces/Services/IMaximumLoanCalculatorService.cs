using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;

namespace CreditCase.Application.Interfaces.Services;

public record MaximumLoanResult(decimal MaximumAmount, int MaximumTerm);

public interface IMaximumLoanCalculatorService
{
    /// <summary>
    /// Müşterinin borç kapasitesi ve risk katsayısına göre alabileceği maksimum tutarı hesaplar.
    /// </summary>
    MaximumLoanResult Calculate(Customer customer, RiskCategory risk, int requestedTerm);
}
