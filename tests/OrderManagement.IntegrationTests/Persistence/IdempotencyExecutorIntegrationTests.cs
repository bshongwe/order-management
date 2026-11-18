using OrderManagement.Application.Orders;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderManagement.IntegrationTests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class IdempotencyExecutorIntegrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task ExecuteAsync_WhenKeyIsReusedForSameRequest_ReplaysOriginalResult()
    {
        await using var dbContext = fixture.CreateDbContext();
        var executor = new SqlServerIdempotencyExecutor(dbContext);
        var executions = 0;

        var first = await executor.ExecuteAsync(
            "status-update-replay",
            "submit:00000000-0000-0000-0000-000000000001",
            _ =>
            {
                executions++;
                return Task.FromResult("submitted");
            });

        var replay = await executor.ExecuteAsync(
            "status-update-replay",
            "submit:00000000-0000-0000-0000-000000000001",
            _ =>
            {
                executions++;
                return Task.FromResult("unexpected");
            });

        Assert.Equal("submitted", first.Value);
        Assert.False(first.Replayed);
        Assert.Equal("submitted", replay.Value);
        Assert.True(replay.Replayed);
        Assert.Equal(1, executions);
    }

    [Fact]
    public async Task ExecuteAsync_WhenKeyIsReusedForDifferentRequest_ThrowsConflict()
    {
        await using var dbContext = fixture.CreateDbContext();
        var executor = new SqlServerIdempotencyExecutor(dbContext);

        await executor.ExecuteAsync(
            "status-update-conflict",
            "submit:00000000-0000-0000-0000-000000000001",
            _ => Task.FromResult("submitted"));

        await Assert.ThrowsAsync<IdempotencyKeyConflictException>(() =>
            executor.ExecuteAsync(
                "status-update-conflict",
                "cancel:00000000-0000-0000-0000-000000000001",
                _ => Task.FromResult("cancelled")));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationFails_DoesNotPersistIdempotencyRecord()
    {
        await using var dbContext = fixture.CreateDbContext();
        var executor = new SqlServerIdempotencyExecutor(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync<string>(
                "status-update-failure",
                "submit:00000000-0000-0000-0000-000000000001",
                _ => throw new InvalidOperationException("Simulated failure.")));

        Assert.False(dbContext.IdempotencyRecords.Any(record => record.Key == "status-update-failure"));
    }
}
