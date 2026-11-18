namespace OrderManagement.Application.Customers;
public sealed record CustomerPage(IReadOnlyCollection<CustomerSummary> Items, int Page, int PageSize, int TotalCount, int TotalPages);
