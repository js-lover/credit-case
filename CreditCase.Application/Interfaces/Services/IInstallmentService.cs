using CreditCase.Application.DTOs.Installments;

namespace CreditCase.Application.Interfaces.Services;

/// <summary>
/// Taksit yönetimi için uygulama katmanı servis sözleşmesi.
/// </summary>
public interface IInstallmentService
{
    /// <summary>
    /// Tüm taksitleri ödeme bilgisiyle birlikte döner.
    /// Dönüşten önce vadesi geçmiş ve <c>Unpaid</c> durumundaki taksitler
    /// otomatik olarak <c>Overdue</c> statüsüne alınır.
    /// </summary>
    Task<IEnumerable<InstallmentResponse>> GetAllAsync();

    /// <summary>
    /// Belirtilen ID'ye sahip taksiti ödeme bilgisiyle birlikte döner.
    /// </summary>
    Task<InstallmentResponse> GetByIdAsync(int id);

    /// <summary>
    /// Taksit durumunu günceller. Sistem tarafından otomatik yapılamayan
    /// manuel durum geçişleri (örn. itiraz sonrası düzeltme) için kullanılır.
    /// </summary>
    Task<InstallmentResponse> UpdateAsync(int id, UpdateInstallmentRequest request);
}
