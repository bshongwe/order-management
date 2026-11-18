namespace OrderManagement.Application.Orders;

public sealed class FulfillOrderHandler(IOrderRepository orderRepository)
{
    public async Task<FulfillOrderResult> HandleAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return new FulfillOrderResult(FulfillOrderOutcome.NotFound, null);
        }

        try
        {
            order.Fulfill();
        }
        catch (InvalidOperationException)
        {
            return new FulfillOrderResult(
                FulfillOrderOutcome.Conflict,
                new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
        }

        await orderRepository.SaveChangesAsync(cancellationToken);

        return new FulfillOrderResult(
            FulfillOrderOutcome.Fulfilled,
            new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
    }
}
