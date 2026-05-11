using CreditCase.Domain.Enums;

namespace CreditCase.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // ── Kredi değerlendirme için profil alanları ────────────────────────────────
    // Bu alanlar risk motoru tarafından kullanılır; LoanEvaluationResult'ta saklanmaz.

    public DateTime DateOfBirth { get; set; }
    public decimal MonthlyIncome { get; set; }
    public ProfessionCategory ProfessionCategory { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }

    // ── Kredi skoru geçmişi bonusu ──────────────────────────────────────────────
    // Zamanında ödeme +5, gecikmiş ödeme -10. Aralık: -200 / +200.
    // MockCreditScoreService bu değeri baz skora ekleyerek nihai skor üretir.

    public int CreditScoreBonus { get; set; } = 0;

    // ── Soft delete ─────────────────────────────────────────────────────────────

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // ── Navigation properties ───────────────────────────────────────────────────

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<LoanEvaluationResult> LoanEvaluations { get; set; } = new List<LoanEvaluationResult>();
}
