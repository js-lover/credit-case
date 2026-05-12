namespace CreditCase.Application.DTOs.LoanEvaluation;

public class InstallmentPlanRow
{
    public int InstallmentNumber { get; set; }
    public decimal PrincipalPayment { get; set; }
    public decimal NetInterest { get; set; }
    public decimal Kkdf { get; set; }
    public decimal Bsmv { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal RemainingBalance { get; set; }
}
