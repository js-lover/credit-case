namespace CreditCase.Application.Exceptions;

public class ExcessiveDebtRatioException : Exception
{
    public decimal ActualRatio { get; }

    public ExcessiveDebtRatioException(decimal ratio)
        : base($"Borç/gelir oranı {ratio:P0}, izin verilen maksimum %70 sınırını aşıyor.")
    {
        ActualRatio = ratio;
    }
}
