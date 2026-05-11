using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCase.Infrastructure.Persistence.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _context;

    public LoanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Loan>> GetAllAsync()
        => await _context.Loans.ToListAsync();

    public async Task<Loan?> GetByIdAsync(int id)
        => await _context.Loans.FindAsync(id);

    public async Task<Loan?> GetByIdWithInstallmentsAsync(int id)
        => await _context.Loans
            .Include(l => l.Installments)
                .ThenInclude(i => i.Payment)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<Loan> AddAsync(Loan loan)
    {
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
        return loan;
    }

    public async Task<Loan> UpdateAsync(Loan loan)
    {
        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
        return loan;
    }
}
