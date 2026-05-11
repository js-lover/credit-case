using CreditCase.Application.DTOs.Payments;
using FluentValidation;

namespace CreditCase.Application.Validators;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.InstallmentId).GreaterThan(0).WithMessage("Geçerli bir taksit ID'si girilmelidir.");
        RuleFor(x => x.PaymentAmount).GreaterThan(0).WithMessage("Ödeme tutarı sıfırdan büyük olmalıdır.");
    }
}
