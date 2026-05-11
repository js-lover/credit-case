using CreditCase.Domain.Entities;

namespace CreditCase.Application.Interfaces.Repositories;

public interface ILoanRepository
{
    Task<IEnumerable<Loan>> GetAllAsync();
    Task<Loan?> GetByIdAsync(int id);
    Task<Loan?> GetByIdWithInstallmentsAsync(int id);
    Task<Loan> AddAsync(Loan loan);
    Task<Loan> UpdateAsync(Loan loan);
}
