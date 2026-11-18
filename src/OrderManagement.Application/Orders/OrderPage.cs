namespace OrderManagement.Application.Orders;

public sealed record OrderPage(
    IReadOnlyList<OrderSummary> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
