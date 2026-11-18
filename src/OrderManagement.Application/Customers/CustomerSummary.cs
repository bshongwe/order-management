namespace OrderManagement.Application.Customers;
public sealed record CustomerSummary(Guid Id, string Name, string Email, string CountryCode, DateTimeOffset CreatedAtUtc);
