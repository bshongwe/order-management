using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Api.HealthChecks;

public sealed class SqlServerReadinessHealthCheck(OrdersDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("SQL Server is reachable.")
            : HealthCheckResult.Unhealthy("SQL Server is unavailable.");
    }
}
