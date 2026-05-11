namespace CreditCase.Application.DTOs.Customers;

public class CustomerSummaryResponse
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int TotalLoans { get; set; }
    public decimal TotalRemainingPrincipal { get; set; }
    public decimal TotalOutstandingDebt { get; set; }
    public int PaidInstallments { get; set; }
    public int UnpaidInstallments { get; set; }
    public int OverdueInstallments { get; set; }
}
