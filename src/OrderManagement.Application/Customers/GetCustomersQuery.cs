namespace OrderManagement.Application.Customers;
public sealed record GetCustomersQuery(string? Search, int Page, int PageSize);
