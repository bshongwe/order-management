namespace OrderManagement.Application.Orders;

public interface IIdempotencyExecutor
{
    Task<IdempotencyExecutionResult<T>> ExecuteAsync<T>(
        string key,
        string requestFingerprint,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
