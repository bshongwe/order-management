using OrderManagement.Domain.Customers;

namespace OrderManagement.Application.Customers;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
    Task<CustomerDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CustomerPage> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
}
