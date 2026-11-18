using System.Text.Json.Serialization;

namespace OrderManagement.Application.Orders;

public sealed record OrderDetails(
    Guid Id,
    string CustomerReference,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid? CustomerId = null,
    string? CurrencyCode = null,
    decimal? TotalAmount = null,
    IReadOnlyList<OrderLineItemDetails>? LineItems = null,
    [property: JsonIgnore] byte[]? RowVersion = null);