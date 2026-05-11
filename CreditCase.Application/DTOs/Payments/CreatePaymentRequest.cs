namespace CreditCase.Application.DTOs.Payments;

public class CreatePaymentRequest
{
    public int InstallmentId { get; set; }
    public decimal PaymentAmount { get; set; }
}
