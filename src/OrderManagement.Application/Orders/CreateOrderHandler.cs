using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public sealed class CreateOrderHandler(IOrderRepository orderRepository)
{
    public async Task<CreateOrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = Order.Create(command.CustomerReference);
        await orderRepository.AddAsync(order, cancellationToken);

        return new CreateOrderResult(
            order.Id,
            order.CustomerReference,
            order.Status.ToString(),
            order.CreatedAtUtc);
    }
}
