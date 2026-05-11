namespace CreditCase.Application.Exceptions;

public class InsufficientCreditScoreException : Exception
{
    public int ActualScore { get; }
    public int MinimumRequired { get; }

    public InsufficientCreditScoreException(int actual, int minimum)
        : base($"Kredi skoru {actual}, gereken minimum skor olan {minimum}'in altında.")
    {
        ActualScore = actual;
        MinimumRequired = minimum;
    }
}
