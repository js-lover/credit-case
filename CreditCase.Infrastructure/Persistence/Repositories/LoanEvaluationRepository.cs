using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCase.Infrastructure.Persistence.Repositories;

public class LoanEvaluationRepository : ILoanEvaluationRepository
{
    private readonly AppDbContext _context;

    public LoanEvaluationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LoanEvaluationResult?> GetByIdAsync(int id)
        => await _context.LoanEvaluations
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

    public async Task<IEnumerable<LoanEvaluationResult>> GetByCustomerIdAsync(int customerId)
        => await _context.LoanEvaluations
            .Where(e => e.CustomerId == customerId && !e.IsDeleted)
            .OrderByDescending(e => e.EvaluationDate)
            .ToListAsync();

    public async Task<LoanEvaluationResult> AddAsync(LoanEvaluationResult result)
    {
        _context.LoanEvaluations.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }
}
