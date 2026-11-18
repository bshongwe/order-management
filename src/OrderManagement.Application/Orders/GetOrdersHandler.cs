namespace OrderManagement.Application.Orders;

public sealed class GetOrdersHandler(IOrderRepository orderRepository)
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public Task<OrderPage> HandleAsync(
        GetOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Page), "Page must be greater than zero.");
        }

        if (query.PageSize is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        if ((long)(query.Page - 1) * query.PageSize > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Page), "The requested page is too large.");
        }

        return orderRepository.GetPageAsync(query.Page, query.PageSize, cancellationToken);
    }
}
