using CreditCase.Application.DTOs.Installments;
using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.Loans;

public class LoanResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public LoanType LoanType { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal RateAmount { get; set; }
    public int Term { get; set; }
    public DateTime StartDate { get; set; }
    public LoanStatus Status { get; set; }
    public decimal RemainingPrincipal { get; set; }
    /// <summary>Faiz dahil toplam geri ödeme tutarı (tüm taksitlerin toplamı).</summary>
    public decimal TotalPayableAmount { get; set; }
    public List<InstallmentResponse> Installments { get; set; } = new();
}
