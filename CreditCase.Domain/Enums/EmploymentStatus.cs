namespace CreditCase.Domain.Enums;

public enum EmploymentStatus
{
    Active,      // Tam zamanlı, sabit çalışan
    PartTime,    // Yarı zamanlı
    Freelance,   // Serbest meslek
    Retired,     // Emekli
    Unemployed   // İşsiz — kredi değerlendirmesinde en yüksek risk
}
