using CreditCase.Application.DTOs.Customers;

namespace CreditCase.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerResponse>> GetAllAsync();
    Task<CustomerResponse> GetByIdAsync(int id);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request);
    Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request);
    Task DeleteAsync(int id);
}
