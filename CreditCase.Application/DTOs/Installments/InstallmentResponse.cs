using CreditCase.Application.DTOs.Payments;
using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.Installments;

public class InstallmentResponse
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public InstallmentStatus Status { get; set; }
    public PaymentResponse? Payment { get; set; }
}
