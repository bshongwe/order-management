namespace OrderManagement.Application.Orders;

public sealed record OrderSummary(
    Guid Id,
    string CustomerReference,
    string Status,
    DateTimeOffset CreatedAtUtc);
