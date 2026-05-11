using CreditCase.Domain.Entities;

namespace CreditCase.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByIdentityNumberAsync(string identityNumber);
    Task<Customer?> GetByEmailAsync(string email);
    Task<Customer?> GetByIdWithLoansAndInstallmentsAsync(int id);
    Task<Customer> AddAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(Customer customer);
}
