using CreditCase.Application.DTOs.Loans;
using CreditCase.Domain.Enums;
using FluentValidation;

namespace CreditCase.Application.Validators;

public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    // claude.md §6A Vade Faktörü tablosundaki tanımlı vadeler.
    private static readonly int[] ValidTerms = [6, 12, 18, 24, 36, 48, 60, 72];

    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Geçerli bir müşteri ID'si girilmelidir.");
        RuleFor(x => x.PrincipalAmount).GreaterThanOrEqualTo(1000).WithMessage("Minimum kredi tutarı 1.000 TL'dir.");
        RuleFor(x => x.PrincipalAmount).LessThanOrEqualTo(1_000_000).WithMessage("Maksimum kredi tutarı 1.000.000 TL'dir.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");

        RuleFor(x => x.Term)
            .Must(t => ValidTerms.Contains(t))
            .WithMessage($"Vade şu değerlerden biri olmalıdır: {string.Join(", ", ValidTerms)} ay.");

        RuleFor(x => x.Term)
            .GreaterThanOrEqualTo(6)
            .When(x => x.IsBalloonPayment)
            .WithMessage("Balon ödeme için en az 6 aylık vade gereklidir.");

        RuleFor(x => x.LoanType)
            .Must(t => t == LoanType.Vehicle)
            .When(x => x.IsBalloonPayment)
            .WithMessage("Balon ödeme yalnızca Araç kredileri için kullanılabilir.");
    }
}
