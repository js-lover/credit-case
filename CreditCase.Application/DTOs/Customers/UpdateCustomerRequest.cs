using CreditCase.Domain.Enums;

namespace CreditCase.Application.DTOs.Customers;

public class UpdateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    // Kredi değerlendirme profili
    public DateTime DateOfBirth { get; set; }
    public decimal MonthlyIncome { get; set; }
    public ProfessionCategory ProfessionCategory { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }
}
