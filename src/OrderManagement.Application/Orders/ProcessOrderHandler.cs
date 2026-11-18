namespace OrderManagement.Application.Orders;

public sealed class ProcessOrderHandler(IOrderRepository orderRepository)
{
    public async Task<ProcessOrderResult> HandleAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return new ProcessOrderResult(ProcessOrderOutcome.NotFound, null);
        }

        try
        {
            order.StartProcessing();
        }
        catch (InvalidOperationException)
        {
            return new ProcessOrderResult(
                ProcessOrderOutcome.Conflict,
                new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
        }

        await orderRepository.SaveChangesAsync(cancellationToken);

        return new ProcessOrderResult(
            ProcessOrderOutcome.ProcessingStarted,
            new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
    }
}
