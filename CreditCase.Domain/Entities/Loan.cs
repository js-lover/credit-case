using CreditCase.Domain.Enums;

namespace CreditCase.Domain.Entities;

public class Loan
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public LoanType LoanType { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int Term { get; set; }
    public DateTime StartDate { get; set; }
    public LoanStatus Status { get; set; }
    public decimal RemainingPrincipal { get; set; }
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}
