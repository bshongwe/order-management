using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class SqlServerIdempotencyExecutor(OrdersDbContext dbContext) : IIdempotencyExecutor
{
    public async Task<IdempotencyExecutionResult<T>> ExecuteAsync<T>(
        string key,
        string requestFingerprint,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprint);
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingRecord = await FindRecordAsync(key, cancellationToken);

        if (existingRecord is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return Replay<T>(existingRecord, requestFingerprint);
        }

        try
        {
            var value = await operation(cancellationToken);
            var responseJson = JsonSerializer.Serialize(value);

            dbContext.IdempotencyRecords.Add(IdempotencyRecord.Create(
                key,
                requestFingerprint,
                typeof(T).FullName ?? typeof(T).Name,
                responseJson));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new IdempotencyExecutionResult<T>(value, Replayed: false);
        }
        catch (Exception exception) when (
            exception is DbUpdateException or OrderConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();

            // A concurrent request using the same key may have committed first.
            // Replay its result; otherwise preserve the original database/concurrency failure.
            var committedRecord = await FindRecordAsync(key, cancellationToken);

            if (committedRecord is not null)
            {
                return Replay<T>(committedRecord, requestFingerprint);
            }

            throw;
        }
    }

    private Task<IdempotencyRecord?> FindRecordAsync(
        string key,
        CancellationToken cancellationToken) =>
        dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(record => record.Key == key, cancellationToken);

    private static IdempotencyExecutionResult<T> Replay<T>(
        IdempotencyRecord record,
        string requestFingerprint)
    {
        if (!string.Equals(record.RequestFingerprint, requestFingerprint, StringComparison.Ordinal) ||
            !string.Equals(
                record.ResponseType,
                typeof(T).FullName ?? typeof(T).Name,
                StringComparison.Ordinal))
        {
            throw new IdempotencyKeyConflictException(
                "The Idempotency-Key has already been used for a different request.");
        }

        var value = JsonSerializer.Deserialize<T>(record.ResponseJson)
            ?? throw new InvalidOperationException("The stored idempotency response could not be deserialized.");

        return new IdempotencyExecutionResult<T>(value, Replayed: true);
    }
}
