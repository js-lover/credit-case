using CreditCase.Application.DTOs.LoanEvaluation;
using FluentValidation;

namespace CreditCase.Application.Validators;

public class LoanApplicationRequestValidator : AbstractValidator<LoanApplicationRequest>
{
    public LoanApplicationRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Müşteri ID'si pozitif bir tam sayı olmalıdır.");

        RuleFor(x => x.RequestedAmount)
            .InclusiveBetween(1_000, 1_000_000)
            .WithMessage("İstenen tutar 1.000 ile 1.000.000 arasında olmalıdır.");

        RuleFor(x => x.RequestedTerm)
            .InclusiveBetween(1, 120)
            .WithMessage("İstenen vade 1 ile 120 ay arasında olmalıdır.");

        RuleFor(x => x.LoanType)
            .IsInEnum().WithMessage("Geçersiz kredi türü.");
    }
}
