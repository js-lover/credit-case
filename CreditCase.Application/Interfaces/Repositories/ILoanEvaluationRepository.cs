using CreditCase.Domain.Entities;

namespace CreditCase.Application.Interfaces.Repositories;

public interface ILoanEvaluationRepository
{
    Task<LoanEvaluationResult?> GetByIdAsync(int id);
    Task<IEnumerable<LoanEvaluationResult>> GetByCustomerIdAsync(int customerId);
    Task<LoanEvaluationResult> AddAsync(LoanEvaluationResult result);
}
