using CreditCase.Application.DTOs.Installments;
using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;

namespace CreditCase.Application.Services;

/// <summary>
/// Taksit sorgulama ve durum güncelleme iş mantığını yönetir.
/// </summary>
public class InstallmentService : IInstallmentService
{
    private readonly IInstallmentRepository _installmentRepository;

    public InstallmentService(IInstallmentRepository installmentRepository)
    {
        _installmentRepository = installmentRepository;
    }

    /// <summary>
    /// Tüm taksitleri döner. Her çağrıda önce vadesi geçmiş taksitler Overdue olarak
    /// işaretlenir. Ayrı bir arka plan servisi yerine bu tetikleme yöntemi tercih edildi;
    /// böylece görev zamanlaması ve ekstra altyapı bağımlılığından kaçınılır.
    /// </summary>
    public async Task<IEnumerable<InstallmentResponse>> GetAllAsync()
    {
        // Overdue güncellemesi listeleme öncesinde yapılmazsa, vadesi geçmiş taksitler
        // hiçbir zaman Overdue durumuna geçmez.
        await _installmentRepository.UpdateOverdueAsync();
        var installments = await _installmentRepository.GetAllAsync();
        return installments.Select(MapToResponse);
    }

    public async Task<InstallmentResponse> GetByIdAsync(int id)
    {
        var installment = await _installmentRepository.GetByIdAsync(id);
        if (installment is null)
            throw new NotFoundException($"{id} numaralı taksit bulunamadı.");
        return MapToResponse(installment);
    }

    public async Task<InstallmentResponse> UpdateAsync(int id, UpdateInstallmentRequest request)
    {
        var installment = await _installmentRepository.GetByIdAsync(id);
        if (installment is null)
            throw new NotFoundException($"{id} numaralı taksit bulunamadı.");

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
        IsBalloon = installment.IsBalloon,
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
