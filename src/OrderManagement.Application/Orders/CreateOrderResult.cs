namespace OrderManagement.Application.Orders;

public sealed record CreateOrderResult(Guid Id, string CustomerReference, string Status, DateTimeOffset CreatedAtUtc);
