namespace CreditCase.Domain.Enums;

/// <summary>
/// Kredi skoru kategorisi (0-1900 arası skor aralıklarına göre).
/// claude.md §6A — Kredi Skoru Kategorileri (Türkiye Bankacılık Standardı).
/// </summary>
public enum ScoreCategory
{
    /// <summary>0 - 969 | Çok Yüksek Risk | Vade Oranı: 5.5-7.2</summary>
    Kritik,

    /// <summary>970 - 1149 | Yüksek Risk | Vade Oranı: 4.5-5.5 | Maks Kredi: Gelir × 3</summary>
    GelisimeAcik,

    /// <summary>1150 - 1469 | Orta Risk | Vade Oranı: 3.5-4.5 | Maks Kredi: Gelir × 10</summary>
    Dengeli,

    /// <summary>1470 - 1719 | Düşük Risk | Vade Oranı: 2.5-3.5 | Maks Kredi: Gelir × 15</summary>
    Guvenli,

    /// <summary>1720 - 1900 | Çok Düşük Risk | Vade Oranı: 1.5-2.5 | Maks Kredi: Gelir × 20</summary>
    Prestijli
}
