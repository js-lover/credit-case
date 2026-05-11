using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.Loans;

public class CreateLoanRequest
{
    public int CustomerId { get; set; }
    public LoanType LoanType { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int Term { get; set; }
    public DateTime StartDate { get; set; }
}
