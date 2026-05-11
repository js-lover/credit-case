using CreditCase.Application.DTOs.Installments;
using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;

namespace CreditCase.Application.Services;

public class InstallmentService : IInstallmentService
{
    private readonly IInstallmentRepository _installmentRepository;

    public InstallmentService(IInstallmentRepository installmentRepository)
    {
        _installmentRepository = installmentRepository;
    }

    public async Task<IEnumerable<InstallmentResponse>> GetAllAsync()
    {
        await _installmentRepository.UpdateOverdueAsync();
        var installments = await _installmentRepository.GetAllAsync();
        return installments.Select(MapToResponse);
    }

    public async Task<InstallmentResponse> GetByIdAsync(int id)
    {
        var installment = await _installmentRepository.GetByIdAsync(id);
        if (installment is null)
            throw new NotFoundException($"Installment with ID {id} not found.");
        return MapToResponse(installment);
    }

    public async Task<InstallmentResponse> UpdateAsync(int id, UpdateInstallmentRequest request)
    {
        var installment = await _installmentRepository.GetByIdAsync(id);
        if (installment is null)
            throw new NotFoundException($"Installment with ID {id} not found.");

        installment.Status = request.Status;
        var updated = await _installmentRepository.UpdateAsync(installment);
        return MapToResponse(updated);
    }

    private static InstallmentResponse MapToResponse(Installment installment) => new()
    {
        Id = installment.Id,
        LoanId = installment.LoanId,
        InstallmentNumber = installment.InstallmentNumber,
        Amount = installment.Amount,
        DueDate = installment.DueDate,
        Status = installment.Status,
        Payment = installment.Payment is null ? null : new PaymentResponse
        {
            Id = installment.Payment.Id,
            InstallmentId = installment.Payment.InstallmentId,
            PaymentAmount = installment.Payment.PaymentAmount,
            PaymentDate = installment.Payment.PaymentDate,
            Status = installment.Payment.Status
        }
    };
}
