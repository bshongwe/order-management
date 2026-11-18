namespace OrderManagement.Application.Orders;

public sealed class ConfirmOrderHandler(IOrderRepository orderRepository)
{
    public async Task<ConfirmOrderResult> HandleAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return new ConfirmOrderResult(ConfirmOrderOutcome.NotFound, null);
        }

        try
        {
            order.Confirm();
        }
        catch (InvalidOperationException)
        {
            return new ConfirmOrderResult(
                ConfirmOrderOutcome.Conflict,
                new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
        }

        await orderRepository.SaveChangesAsync(cancellationToken);

        return new ConfirmOrderResult(
            ConfirmOrderOutcome.Confirmed,
            new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
    }
}
