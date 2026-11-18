namespace OrderManagement.Application.Customers;
public sealed class GetCustomerByIdHandler(ICustomerRepository repository)
{
    public Task<CustomerDetails?> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(id, cancellationToken);
}
