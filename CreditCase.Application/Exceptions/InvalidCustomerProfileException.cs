namespace CreditCase.Application.Exceptions;

public class InvalidCustomerProfileException : Exception
{
    public InvalidCustomerProfileException(string reason)
        : base($"Müşteri profili geçersiz: {reason}") { }
}
