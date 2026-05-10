using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=CreditCaseDb;User Id=sa;Password=StrongPass123!;TrustServerCertificate=True;"
        );

        return new AppDbContext(optionsBuilder.Options);
    }
}