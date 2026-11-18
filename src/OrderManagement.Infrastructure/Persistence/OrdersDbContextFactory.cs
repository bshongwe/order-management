using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderManagement.Infrastructure.Persistence;

/// <summary>
/// Provides EF Core tooling with the same SQL Server provider used at runtime.
/// The connection string is read from the environment and is never persisted in source.
/// </summary>
public sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Orders");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Orders' is not configured. Set ConnectionStrings__Orders before running EF Core tools.");
        }

        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new OrdersDbContext(options);
    }
}
