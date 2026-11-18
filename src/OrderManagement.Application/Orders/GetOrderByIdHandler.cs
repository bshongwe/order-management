namespace OrderManagement.Application.Orders;

public sealed class GetOrderByIdHandler(IOrderRepository orderRepository)
{
    public Task<OrderDetails?> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return orderRepository.GetByIdAsync(query.Id, cancellationToken);
    }
}
