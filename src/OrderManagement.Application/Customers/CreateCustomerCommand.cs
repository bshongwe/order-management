namespace OrderManagement.Application.Customers;
public sealed record CreateCustomerCommand(string Name, string Email, string CountryCode);
