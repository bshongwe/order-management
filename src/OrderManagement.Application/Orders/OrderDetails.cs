namespace OrderManagement.Application.Orders;

public sealed record OrderDetails(
    Guid Id,
    string CustomerReference,
    string Status,
    DateTimeOffset CreatedAtUtc);
