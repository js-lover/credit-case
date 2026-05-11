using CreditCase.Application.DTOs.Payments;

namespace CreditCase.Application.Interfaces.Services;

/// <summary>
/// Ödeme yönetimi için uygulama katmanı servis sözleşmesi.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Sistemdeki tüm ödemeleri döner.
    /// </summary>
    Task<IEnumerable<PaymentResponse>> GetAllAsync();

    /// <summary>
    /// Belirtilen taksit için ödeme gerçekleştirir.
    /// <list type="bullet">
    ///   <item>Zaten <c>Paid</c> olan taksit tekrar ödenemez.</item>
    ///   <item>Aynı taksit için ikinci bir ödeme kaydı oluşturulamaz.</item>
    ///   <item>Başarılı ödeme sonrası kredinin <c>RemainingPrincipal</c> değeri yeniden hesaplanır.</item>
    ///   <item>Tüm taksitler ödendiğinde kredi <c>Closed</c> statüsüne geçer.</item>
    /// </list>
    /// </summary>
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request);
}
