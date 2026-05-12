using CreditCase.Domain.Enums;

namespace CreditCase.Domain.Entities;

/// <summary>
/// Kredi başvurusu değerlendirme kaydı. Her başvuru bağımsız bir kayıt üretir;
/// finans sektöründe denetim izi zorunlu olduğundan silinmez, soft delete uygulanır.
/// </summary>
public class LoanEvaluationResult
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // ── Başvuru parametreleri ───────────────────────────────────────────────────

    public decimal RequestedAmount { get; set; }
    public int RequestedTerm { get; set; }          // ay cinsinden
    public LoanType RequestedLoanType { get; set; }

    // ── Değerlendirme sonuçları ─────────────────────────────────────────────────

    public bool IsApproved { get; set; }
    public decimal ApprovedAmount { get; set; }     // Red durumunda 0
    public decimal MaximumAmount { get; set; }      // Müşterinin alabileceği üst sınır
    public int MaximumTerm { get; set; }            // Ay cinsinden maksimum vade
    public decimal ApprovedRateAmount { get; set; }
    public RiskCategory RiskLevel { get; set; }
    public int CreditScore { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    public decimal MonthlyInstallmentEstimate { get; set; }
    public string? RejectionReason { get; set; }   // Yalnızca IsApproved=false durumunda

    // ── Tarih alanları ──────────────────────────────────────────────────────────

    public DateTime EvaluationDate { get; set; }
    /// <summary>Onay geçerlilik tarihi — onaylanmış başvurular 30 gün geçerlidir.</summary>
    public DateTime ExpirationDate { get; set; }

    // ── Soft delete ─────────────────────────────────────────────────────────────

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
