using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCase.Infrastructure.Persistence.Repositories;

/// <summary>
/// Müşteri veri erişim katmanı. Soft delete dahil tüm CRUD operasyonlarını uygular.
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    // EF Core global query filter (HasQueryFilter) devrede olduğundan
    // tüm sorgular otomatik olarak WHERE IsDeleted = 0 filtresiyle çalışır.

    public async Task<IEnumerable<Customer>> GetAllAsync()
        => await _context.Customers.ToListAsync();

    public async Task<Customer?> GetByIdAsync(int id)
        => await _context.Customers.FindAsync(id);

    public async Task<Customer?> GetByIdentityNumberAsync(string identityNumber)
        => await _context.Customers.FirstOrDefaultAsync(c => c.IdentityNumber == identityNumber);

    public async Task<Customer?> GetByEmailAsync(string email)
        => await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);

    /// <summary>
    /// Müşteriyi kredi ve taksit detaylarıyla birlikte getirir.
    /// Borç özeti gibi taksit düzeyinde agregasyon gerektiren senaryolarda kullanılır.
    /// </summary>
    public async Task<Customer?> GetByIdWithLoansAndInstallmentsAsync(int id)
        => await _context.Customers
            .Include(c => c.Loans)
                .ThenInclude(l => l.Installments)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Customer> AddAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    /// <summary>
    /// Müşteriyi fiziksel olarak silmez; IsDeleted = true ve DeletedAt set ederek günceller.
    /// Finans sektöründe kredi/ödeme geçmişinin korunması zorunludur.
    /// Global query filter sayesinde bu kayıt sonraki sorgularda görünmez.
    /// </summary>
    public async Task DeleteAsync(Customer customer)
    {
        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }
}
