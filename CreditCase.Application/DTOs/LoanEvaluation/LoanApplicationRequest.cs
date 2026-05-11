using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.LoanEvaluation;

public class LoanApplicationRequest
{
    public int CustomerId { get; set; }
    public LoanType LoanType { get; set; }
    public decimal RequestedAmount { get; set; }
    public int RequestedTerm { get; set; }  // ay cinsinden
}
