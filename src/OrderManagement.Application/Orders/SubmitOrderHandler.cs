namespace OrderManagement.Application.Orders;

public sealed class SubmitOrderHandler(IOrderRepository orderRepository)
{
    public async Task<SubmitOrderResult> HandleAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return new SubmitOrderResult(SubmitOrderOutcome.NotFound, null);
        }

        try
        {
            order.Submit();
        }
        catch (InvalidOperationException)
        {
            return new SubmitOrderResult(
                SubmitOrderOutcome.Conflict,
                new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
        }

        await orderRepository.SaveChangesAsync(cancellationToken);

        return new SubmitOrderResult(
            SubmitOrderOutcome.Submitted,
            new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
    }
}
