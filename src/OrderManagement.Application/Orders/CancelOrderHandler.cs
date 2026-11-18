using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public sealed class CancelOrderHandler(IOrderRepository orderRepository)
{
    public async Task<CancelOrderResult> HandleAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return new CancelOrderResult(CancelOrderOutcome.NotFound, null);
        }

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException)
        {
            return new CancelOrderResult(
                CancelOrderOutcome.Conflict,
                new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
        }

        await orderRepository.SaveChangesAsync(cancellationToken);

        return new CancelOrderResult(
            CancelOrderOutcome.Cancelled,
            new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
    }
}
