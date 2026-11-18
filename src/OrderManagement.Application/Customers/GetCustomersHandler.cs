namespace OrderManagement.Application.Customers;

public sealed class GetCustomersHandler(ICustomerRepository repository)
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public Task<CustomerPage> HandleAsync(GetCustomersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page < 1)
            throw new ArgumentOutOfRangeException(nameof(query.Page), "Page must be greater than zero.");
        if (query.PageSize is < 1 or > MaximumPageSize)
            throw new ArgumentOutOfRangeException(nameof(query.PageSize), $"Page size must be between 1 and {MaximumPageSize}.");
        return repository.GetPageAsync(query.Search?.Trim(), query.Page, query.PageSize, cancellationToken);
    }
}
