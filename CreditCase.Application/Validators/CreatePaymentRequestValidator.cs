using CreditCase.Application.DTOs.Payments;
using FluentValidation;

namespace CreditCase.Application.Validators;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.InstallmentId).GreaterThan(0).WithMessage("A valid installment ID is required.");
        RuleFor(x => x.PaymentAmount).GreaterThan(0).WithMessage("Payment amount must be greater than 0.");
    }
}
