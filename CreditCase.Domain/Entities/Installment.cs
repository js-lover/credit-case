using CreditCase.Domain.Enums;

namespace CreditCase.Domain.Entities;

public class Installment
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public Loan Loan { get; set; } = null!;
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public InstallmentStatus Status { get; set; }
    public Payment? Payment { get; set; }
}
