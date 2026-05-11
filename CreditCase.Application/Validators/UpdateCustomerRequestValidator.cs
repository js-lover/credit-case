using CreditCase.Application.DTOs.Customers;
using FluentValidation;

namespace CreditCase.Application.Validators;

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Ad alanı zorunludur.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Soyad alanı zorunludur.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta alanı zorunludur.")
            .EmailAddress().WithMessage("Geçersiz e-posta formatı.");
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası zorunludur.")
            .Matches(@"^\d{10,11}$").WithMessage("Telefon numarası 10 veya 11 haneli rakamlardan oluşmalıdır.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Doğum tarihi zorunludur.")
            .LessThan(DateTime.UtcNow.AddYears(-18)).WithMessage("Müşteri en az 18 yaşında olmalıdır.");

        RuleFor(x => x.MonthlyIncome)
            .GreaterThan(0).WithMessage("Aylık gelir sıfırdan büyük olmalıdır.");

        RuleFor(x => x.ProfessionCategory)
            .IsInEnum().WithMessage("Geçersiz meslek kategorisi.");

        RuleFor(x => x.EmploymentStatus)
            .IsInEnum().WithMessage("Geçersiz istihdam durumu.");
    }
}
