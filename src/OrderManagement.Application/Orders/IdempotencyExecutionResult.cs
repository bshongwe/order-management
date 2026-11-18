namespace OrderManagement.Application.Orders;

public sealed record IdempotencyExecutionResult<T>(T Value, bool Replayed);
