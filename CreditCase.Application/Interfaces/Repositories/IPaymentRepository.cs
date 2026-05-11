using CreditCase.Domain.Entities;

namespace CreditCase.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<Payment?> GetByInstallmentIdAsync(int installmentId);
    Task<Payment> AddAsync(Payment payment);
}
