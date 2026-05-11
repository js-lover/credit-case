using CreditCase.Domain.Entities;
using CreditCase.Domain.Interfaces;

namespace CreditCase.Infrastructure.Services.Rules;

/// <summary>
/// Yaş bileşeni. CLAUDE.md: (YaşPuanı × 0.15)
/// 25-55 yaş arası en düşük riskli grup; 21 altı ve 65 üstü risk artar.
/// </summary>
public class AgeRule : IRiskAnalysisRule
{
    public decimal Weight => 0.15m;

    public decimal Evaluate(Customer customer, decimal requestedAmount, int requestedTerm, int creditScore)
    {
        int age = CalculateAge(customer.DateOfBirth);

        return age switch
        {
            < 21          => 0,
            < 23          => 30,
            < 25          => 50,
            <= 55         => 100,
            <= 60         => 80,
            <= 65         => 50,
            _             => 0
        };
    }

    private static int CalculateAge(DateTime dob)
    {
        var today = DateTime.UtcNow;
        int age = today.Year - dob.Year;
        if (today < dob.AddYears(age)) age--;
        return age;
    }
}
