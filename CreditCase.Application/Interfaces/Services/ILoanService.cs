using CreditCase.Application.DTOs.Loans;

namespace CreditCase.Application.Interfaces.Services;

/// <summary>
/// Kredi yönetimi için uygulama katmanı servis sözleşmesi.
/// </summary>
public interface ILoanService
{
    /// <summary>
    /// Sistemdeki tüm kredileri döner. Taksit detayları dahil değildir.
    /// </summary>
    Task<IEnumerable<LoanResponse>> GetAllAsync();

    /// <summary>
    /// Belirtilen ID'ye sahip krediyi taksit planı ve ödeme bilgileriyle birlikte döner.
    /// </summary>
    Task<LoanResponse> GetByIdAsync(int id);

    /// <summary>
    /// Yeni kredi oluşturur ve taksit planını otomatik üretir.
    /// Oluşturma öncesi <c>CreditScoreService</c> üzerinden kredi skoru sorgulanır;
    /// sonuç <c>Approved</c> değilse <see cref="Exceptions.BusinessRuleException"/> fırlatılır.
    /// Taksit tutarı düz vade farkı (flat-rate) yöntemiyle hesaplanır:
    /// <c>monthlyAmount = ROUND(principal × (1 + rate/100 × termYears) / term, 2)</c>
    /// </summary>
    Task<LoanResponse> CreateAsync(CreateLoanRequest request);
}
