using OrderManagement.Domain.Customers;

namespace OrderManagement.Application.Customers;

public sealed class CreateCustomerHandler(ICustomerRepository repository)
{
    public async Task<CustomerDetails> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var customer = Customer.Create(command.Name, command.Email, command.CountryCode);
        await repository.AddAsync(customer, cancellationToken);
        return new CustomerDetails(customer.Id, customer.Name, customer.Email, customer.CountryCode, customer.CreatedAtUtc);
    }
}
