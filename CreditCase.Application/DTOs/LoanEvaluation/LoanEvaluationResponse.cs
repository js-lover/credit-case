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
    public decimal ApprovedInterestRate { get; set; }

    // ── Risk profili ─────────────────────────────────────────────────────────────
    public RiskCategory RiskLevel { get; set; }
    public int CreditScore { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    public decimal MonthlyInstallmentEstimate { get; set; }

    public string? RejectionReason { get; set; }
    public DateTime EvaluationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
}
