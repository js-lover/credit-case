using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.Payments;

public class PaymentResponse
{
    public int Id { get; set; }

    // Taksit bağlamı
    public int InstallmentId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }

    // Kredi bağlamı
    public int LoanId { get; set; }
    public LoanType LoanType { get; set; }

    // Müşteri bağlamı
    public int CustomerId { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;

    // Ödeme verisi
    public decimal PaymentAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentStatus Status { get; set; }
}
