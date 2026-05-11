using CreditCase.Domain.Enums;

namespace CreditCase.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int InstallmentId { get; set; }
    public Installment Installment { get; set; } = null!;
    public decimal PaymentAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentStatus Status { get; set; }
}
