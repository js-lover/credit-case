using CreditCase.Application.DTOs.Loans;
using FluentValidation;

namespace CreditCase.Application.Validators;

public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("A valid customer ID is required.");
        RuleFor(x => x.PrincipalAmount).GreaterThan(0).WithMessage("Principal amount must be greater than 0.");
        RuleFor(x => x.InterestRate).GreaterThanOrEqualTo(0).WithMessage("Interest rate cannot be negative.");
        RuleFor(x => x.Term).GreaterThanOrEqualTo(1).WithMessage("Term must be at least 1 month.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
    }
}
