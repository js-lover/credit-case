using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CreditCase.Infrastructure.Persistence.Repositories;

/// <summary>
/// Taksit veri erişim katmanı. Overdue toplu güncelleme dahil.
/// </summary>
public class InstallmentRepository : IInstallmentRepository
{
    private readonly AppDbContext _context;

    public InstallmentRepository(AppDbContext context)
    {
        _context = context;
    }

    // Ödeme bilgisi taksit sorgularında her zaman gerektiği için Payment eager load edilir.

    public async Task<IEnumerable<Installment>> GetAllAsync()
        => await _context.Installments
            .Include(i => i.Payment)
            .ToListAsync();

    public async Task<Installment?> GetByIdAsync(int id)
        => await _context.Installments
            .Include(i => i.Payment)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Installment> UpdateAsync(Installment installment)
    {
        _context.Installments.Update(installment);
        await _context.SaveChangesAsync();
        return installment;
    }

    /// <summary>
    /// Vadesi geçmiş ve ödenmemiş tüm taksitleri tek bir SQL UPDATE ile Overdue yapar.
    /// <c>ExecuteUpdateAsync</c>, entity'leri belleğe yüklemeden doğrudan veritabanında
    /// çalışır; yüksek hacimli tablolarda performans avantajı sağlar.
    /// </summary>
    public async Task UpdateOverdueAsync()
    {
        var now = DateTime.UtcNow;
        await _context.Installments
            .Where(i => i.Status == InstallmentStatus.Unpaid && i.DueDate < now)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, InstallmentStatus.Overdue));
    }
}
