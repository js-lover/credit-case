namespace CreditCase.Domain.Enums;

/// <summary>
/// Kredi skoru kategorisi için yardımcı metotlar.
/// claude.md §6A tablolarından türetilen sınır ve katsayılar.
/// </summary>
public static class ScoreCategoryHelper
{
    /// <summary>Kredi skoru (0-1900) → ScoreCategory dönüşümü.</summary>
    public static ScoreCategory FromScore(int creditScore) => creditScore switch
    {
        >= 1720 => ScoreCategory.Prestijli,
        >= 1470 => ScoreCategory.Guvenli,
        >= 1150 => ScoreCategory.Dengeli,
        >= 970  => ScoreCategory.GelisimeAcik,
        _       => ScoreCategory.Kritik
    };

    /// <summary>ScoreCategory → RiskCategory (entity RiskLevel alanı için).</summary>
    public static RiskCategory ToRiskCategory(ScoreCategory category) => category switch
    {
        ScoreCategory.Prestijli    => RiskCategory.Low,
        ScoreCategory.Guvenli      => RiskCategory.Low,
        ScoreCategory.Dengeli      => RiskCategory.Medium,
        ScoreCategory.GelisimeAcik => RiskCategory.High,
        _                          => RiskCategory.VeryHigh  // Kritik
    };

    /// <summary>Kategoriye göre maksimum vade (ay).</summary>
    public static int MaxTermMonths(ScoreCategory category) => category switch
    {
        ScoreCategory.Prestijli    => 72,
        ScoreCategory.Guvenli      => 60,
        ScoreCategory.Dengeli      => 48,
        ScoreCategory.GelisimeAcik => 36,
        _                          => 24  // Kritik
    };

    /// <summary>Kategoriye göre gelir katsayısı (maksimum kredi = gelir × katsayı).</summary>
    public static decimal IncomeMultiplier(ScoreCategory category) => category switch
    {
        ScoreCategory.Prestijli    => 20m,
        ScoreCategory.Guvenli      => 15m,
        ScoreCategory.Dengeli      => 10m,
        ScoreCategory.GelisimeAcik => 3m,
        _                          => 0m  // Kritik — reddedilir
    };

    /// <summary>Kategoriye göre minimum aylık taksit tutarı (TL).</summary>
    public static decimal MinInstallmentAmount(ScoreCategory category) => category switch
    {
        ScoreCategory.Kritik       => 50_000m,
        ScoreCategory.GelisimeAcik => 25_000m,
        ScoreCategory.Dengeli      => 15_000m,
        ScoreCategory.Guvenli      => 10_000m,
        ScoreCategory.Prestijli    => 5_000m,
        _                          => 50_000m
    };
}
