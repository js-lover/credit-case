using CreditCase.Domain.Entities;

namespace CreditCase.Application.Interfaces.Repositories;

public interface IInstallmentRepository
{
    Task<IEnumerable<Installment>> GetAllAsync();
    Task<Installment?> GetByIdAsync(int id);
    Task<Installment> UpdateAsync(Installment installment);
    Task UpdateOverdueAsync();
}
