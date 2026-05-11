using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCase.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;
    
    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
        => await _context.Payments
            .Include(p => p.Installment)
                .ThenInclude(i => i.Loan)
                    .ThenInclude(l => l.Customer)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

    public async Task<Payment?> GetByInstallmentIdAsync(int installmentId)
        => await _context.Payments.FirstOrDefaultAsync(p => p.InstallmentId == installmentId);

    public async Task<Payment> AddAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}
