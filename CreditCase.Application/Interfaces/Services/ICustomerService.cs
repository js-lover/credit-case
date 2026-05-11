using CreditCase.Application.DTOs.Customers;

namespace CreditCase.Application.Interfaces.Services;

/// <summary>
/// Müşteri yönetimi için uygulama katmanı servis sözleşmesi.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Sistemdeki tüm müşterileri döner.
    /// </summary>
    Task<IEnumerable<CustomerResponse>> GetAllAsync();

    /// <summary>
    /// Belirtilen ID'ye sahip müşteriyi döner.
    /// Müşteri bulunamazsa <see cref="Exceptions.NotFoundException"/> fırlatır.
    /// </summary>
    Task<CustomerResponse> GetByIdAsync(int id);

    /// <summary>
    /// Yeni müşteri kaydı oluşturur.
    /// TC Kimlik Numarası sistemde benzersiz olmalıdır; tekrar edilirse
    /// <see cref="Exceptions.BusinessRuleException"/> fırlatılır.
    /// </summary>
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request);

    /// <summary>
    /// Müşterinin ad, soyad, e-posta ve telefon bilgilerini günceller.
    /// TC Kimlik Numarası değiştirilemez.
    /// </summary>
    Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request);

    /// <summary>
    /// Müşteriyi siler. Bağlı tüm kredi ve taksit kayıtları cascade olarak kaldırılır.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Müşterinin borç özetini döner: toplam kalan anapara, vadesi geçmiş/ödenen/bekleyen
    /// taksit sayıları ve toplam ödenmemiş borç tutarı.
    /// </summary>
    Task<CustomerSummaryResponse> GetSummaryAsync(int id);
}
