namespace CreditCase.Application.Exceptions;

/// <summary>
/// Risk analizi sonucu kredi başvurusu reddedildiğinde fırlatılır.
/// HTTP 422 Unprocessable Entity ile eşleştirilir.
/// </summary>
public class LoanApplicationDeniedException : Exception
{
    public string DenialReason { get; }

    public LoanApplicationDeniedException(string reason)
        : base($"Kredi başvurusu reddedildi: {reason}")
    {
        DenialReason = reason;
    }
}
