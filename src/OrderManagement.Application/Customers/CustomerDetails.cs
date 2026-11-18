namespace OrderManagement.Application.Customers;
public sealed record CustomerDetails(Guid Id, string Name, string Email, string CountryCode, DateTimeOffset CreatedAtUtc);
