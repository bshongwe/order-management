using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderManagement.Api.HealthChecks;
using OrderManagement.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderManagement.IntegrationTests.HealthChecks;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerReadinessHealthCheckTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task CheckHealthAsync_WhenSqlServerIsAvailable_ReturnsHealthy()
    {
        await using var dbContext = fixture.CreateDbContext();
        var healthCheck = new SqlServerReadinessHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "sql-server",
                    _ => healthCheck,
                    HealthStatus.Unhealthy,
                    ["ready"])
            },
            CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
