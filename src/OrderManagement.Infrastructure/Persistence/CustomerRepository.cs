using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Customers;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class CustomerRepository(OrdersDbContext dbContext) : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        await dbContext.Customers.AddAsync(customer, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<CustomerDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Customers.AsNoTracking()
            .Where(customer => customer.Id == id)
            .Select(customer => new CustomerDetails(customer.Id, customer.Name, customer.Email, customer.CountryCode, customer.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CustomerPage> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var customers = dbContext.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            customers = customers.Where(customer => customer.Name.Contains(search) || customer.Email.Contains(search));

        var totalCount = await customers.CountAsync(cancellationToken);
        var items = await customers
            .OrderBy(customer => customer.Name).ThenBy(customer => customer.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(customer => new CustomerSummary(customer.Id, customer.Name, customer.Email, customer.CountryCode, customer.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new CustomerPage(items, page, pageSize, totalCount, totalPages);
    }
}
