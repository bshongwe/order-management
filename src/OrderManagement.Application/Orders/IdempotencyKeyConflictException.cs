namespace OrderManagement.Application.Orders;

public sealed class IdempotencyKeyConflictException(string message)
    : Exception(message)
{
}
