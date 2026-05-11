using CreditCase.Domain.Entities;

namespace CreditCase.Domain.Interfaces;

/// <summary>
/// Her risk analiz kuralının implement etmesi gereken arayüz (Rule Engine pattern).
/// Yeni kural = yeni sınıf; mevcut motoru değiştirmeden sisteme eklenir.
/// </summary>
public interface IRiskAnalysisRule
{
    /// <summary>
    /// Kuralın bu müşteri/başvuru için risk puanına olan katkısını (0-100) döner.
    /// Dönen değer motordaki ağırlıkla çarpılarak toplam skora eklenir.
    /// </summary>
    decimal Evaluate(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore);

    /// <summary>Kuralın toplam skorlamadaki ağırlık katsayısı (örn: 0.30).</summary>
    decimal Weight { get; }
}
