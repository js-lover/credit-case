using CreditCase.Application.DTOs.LoanEvaluation;

namespace CreditCase.Application.Interfaces.Services;

public interface ILoanEvaluationService
{
    Task<LoanEvaluationResponse> EvaluateAsync(LoanApplicationRequest request);
    Task<LoanEvaluationResponse> GetByIdAsync(int evaluationId);
    Task<IEnumerable<LoanEvaluationResponse>> GetByCustomerIdAsync(int customerId);
    Task<LoanEvaluationResponse> GetMaximumEligibilityAsync(int customerId);
}
