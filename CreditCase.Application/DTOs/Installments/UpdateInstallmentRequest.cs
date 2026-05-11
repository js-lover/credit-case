using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.Installments;

public class UpdateInstallmentRequest
{
    public InstallmentStatus Status { get; set; }
}
