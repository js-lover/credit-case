using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.LoanEvaluation;

public class LoanEvaluationResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;

    // ── Başvuru ─────────────────────────────────────────────────────────────────
    public decimal RequestedAmount { get; set; }
    public int RequestedTerm { get; set; }
    public LoanType RequestedLoanType { get; set; }

    // ── Karar ───────────────────────────────────────────────────────────────────
    public bool IsApproved { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public int MaximumTerm { get; set; }
    /// <summary>Onaylanan vade oranı (ratio formatı, örn: 3.25). Yüzde değil.</summary>
    public decimal ApprovedRateAmount { get; set; }

    // ── Risk profili ─────────────────────────────────────────────────────────────
    /// <summary>4 değerli risk seviyesi (Low/Medium/High/VeryHigh) — entity alanı.</summary>
    public RiskCategory RiskLevel { get; set; }
    /// <summary>5 değerli kredi skoru kategorisi — oran ve limit hesabı için.</summary>
    public ScoreCategory CreditScoreCategory { get; set; }
    public int CreditScore { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    public decimal MonthlyInstallmentEstimate { get; set; }

    // ── Vergi ve maliyet göstergeleri ────────────────────────────────────────────
    /// <summary>KKDF+BSMV dahil brüt aylık oran (örn: 4.692).</summary>
    public decimal GrossRateAmount { get; set; }
    /// <summary>YMO — Yıllık Maliyet Oranı, brüt efektif yıllık oran (%).</summary>
    public decimal AnnualCostRate { get; set; }
    /// <summary>Onaylı kredinin taksit bazlı amortisman tablosu (boş liste = reddedildi).</summary>
    public List<InstallmentPlanRow> InstallmentPlan { get; set; } = [];

    public string? RejectionReason { get; set; }
    public DateTime EvaluationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
}
