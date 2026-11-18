using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
using Xunit;

namespace OrderManagement.UnitTests.Orders;

public sealed class FulfillOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenOrderIsProcessing_FulfillsAndPersistsOrder()
    {
        var order = Order.Create("CUSTOMER-100");
        order.Submit();
        order.Confirm();
        order.StartProcessing();
        var repository = new InMemoryOrderRepository([order]);
        var handler = new FulfillOrderHandler(repository);

        var result = await handler.HandleAsync(order.Id);

        Assert.Equal(FulfillOrderOutcome.Fulfilled, result.Outcome);
        Assert.NotNull(result.Order);
        Assert.Equal(nameof(OrderStatus.Fulfilled), result.Order.Status);
        Assert.Equal(OrderStatus.Fulfilled, order.Status);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var repository = new InMemoryOrderRepository([]);
        var handler = new FulfillOrderHandler(repository);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Equal(FulfillOrderOutcome.NotFound, result.Outcome);
        Assert.Null(result.Order);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Submitted")]
    [InlineData("Confirmed")]
    [InlineData("Fulfilled")]
    [InlineData("Cancelled")]
    public async Task HandleAsync_WhenOrderIsNotProcessing_ReturnsConflictWithoutSaving(string status)
    {
        var order = Order.Create("CUSTOMER-100");

        switch (status)
        {
            case "Submitted":
                order.Submit();
                break;
            case "Confirmed":
                order.Submit();
                order.Confirm();
                break;
            case "Fulfilled":
                order.Submit();
                order.Confirm();
                order.StartProcessing();
                order.Fulfill();
                break;
            case "Cancelled":
                order.Cancel();
                break;
        }

        var repository = new InMemoryOrderRepository([order]);
        var handler = new FulfillOrderHandler(repository);

        var result = await handler.HandleAsync(order.Id);

        Assert.Equal(FulfillOrderOutcome.Conflict, result.Outcome);
        Assert.NotNull(result.Order);
        Assert.Equal(status, result.Order.Status);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    private sealed class InMemoryOrderRepository(IReadOnlyList<Order> orders) : IOrderRepository
    {
        public int SaveChangesCount { get; private set; }

        public Task AddAsync(Order order, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OrderDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(orders.SingleOrDefault(order => order.Id == id));

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }

        public Task<OrderPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
