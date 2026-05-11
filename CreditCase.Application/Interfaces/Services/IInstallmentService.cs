using CreditCase.Application.DTOs.Installments;

namespace CreditCase.Application.Interfaces.Services;

public interface IInstallmentService
{
    Task<IEnumerable<InstallmentResponse>> GetAllAsync();
    Task<InstallmentResponse> GetByIdAsync(int id);
    Task<InstallmentResponse> UpdateAsync(int id, UpdateInstallmentRequest request);
}
