using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.Payments;

public class PaymentResponse
{
    public int Id { get; set; }
    public int InstallmentId { get; set; }
    public decimal PaymentAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentStatus Status { get; set; }
}
