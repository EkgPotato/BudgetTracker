using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finance.Infrastructure.Data;

public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FinanceDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Set ConnectionStrings__FinanceDb before running Entity Framework design-time commands.");
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FinanceDbContext(options);
    }
}
