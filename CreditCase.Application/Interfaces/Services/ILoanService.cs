using CreditCase.Application.DTOs.Loans;

namespace CreditCase.Application.Interfaces.Services;

public interface ILoanService
{
    Task<IEnumerable<LoanResponse>> GetAllAsync();
    Task<LoanResponse> GetByIdAsync(int id);
    Task<LoanResponse> CreateAsync(CreateLoanRequest request);
}
