namespace OrderManagement.Application.Orders;

public sealed record GetOrdersQuery(int Page = 1, int PageSize = 20);
