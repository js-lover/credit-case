using CreditCase.Application.DTOs.Loans;
using CreditCase.Domain.Enums;
using FluentValidation;

namespace CreditCase.Application.Validators;

public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Geçerli bir müşteri ID'si girilmelidir.");
        RuleFor(x => x.PrincipalAmount).GreaterThanOrEqualTo(1000).WithMessage("Minimum kredi tutarı 1.000 TL'dir.");
        RuleFor(x => x.PrincipalAmount).LessThanOrEqualTo(1_000_000).WithMessage("Maksimum kredi tutarı 1.000.000 TL'dir.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");

        RuleFor(x => x.Term)
            .Must(t => new[] { 3, 6, 12, 24, 36, 48, 60, 84, 120 }.Contains(t))
            .WithMessage("Vade şu değerlerden biri olmalıdır: 3, 6, 12, 24, 36, 48, 60, 84 veya 120 ay.");

        // Tutara göre maksimum vade kısıtlaması: küçük kredilere uzun vade uygulanamaz.
        RuleFor(x => x.Term)
            .Must((req, term) => term <= GetMaxTermForAmount(req.PrincipalAmount))
            .WithMessage(req =>
                $"Seçilen vade, {req.PrincipalAmount:N0} TL kredi için uygun değil. " +
                $"Bu tutar için maksimum vade: {GetMaxTermForAmount(req.PrincipalAmount)} ay.");

        // Balon ödeme için en az 6 ay vade gerekli
        RuleFor(x => x.Term)
            .GreaterThanOrEqualTo(6)
            .When(x => x.IsBalloonPayment)
            .WithMessage("Balon ödeme için en az 6 aylık vade gereklidir.");

        RuleFor(x => x.LoanType)
            .Must(t => t == LoanType.Vehicle)
            .When(x => x.IsBalloonPayment)
            .WithMessage("Balon ödeme yalnızca Araç kredileri için kullanılabilir.");
    }

    // ≤10K TL → 24 ay, ≤50K → 60 ay, ≤150K → 84 ay, üzeri → 120 ay
    public static int GetMaxTermForAmount(decimal amount) => amount switch
    {
        <= 10_000m  => 24,
        <= 50_000m  => 60,
        <= 150_000m => 84,
        _           => 120
    };
}
