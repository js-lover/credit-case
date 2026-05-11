using CreditCase.Application.DTOs.Payments;

namespace CreditCase.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<IEnumerable<PaymentResponse>> GetAllAsync();
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request);
}
