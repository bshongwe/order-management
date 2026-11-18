using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
using Xunit;

namespace OrderManagement.UnitTests.Orders;

public sealed class CancelOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenOrderIsDraft_CancelsAndPersistsOrder()
    {
        var order = Order.Create("CUSTOMER-100");
        var repository = new InMemoryOrderRepository([order]);
        var handler = new CancelOrderHandler(repository);

        var result = await handler.HandleAsync(order.Id);

        Assert.Equal(CancelOrderOutcome.Cancelled, result.Outcome);
        Assert.NotNull(result.Order);
        Assert.Equal(nameof(OrderStatus.Cancelled), result.Order.Status);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var repository = new InMemoryOrderRepository([]);
        var handler = new CancelOrderHandler(repository);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Equal(CancelOrderOutcome.NotFound, result.Outcome);
        Assert.Null(result.Order);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderIsFulfilled_ReturnsConflictWithoutSaving()
    {
        var order = Order.Create("CUSTOMER-100");
        order.Submit();
        order.Cancel();
        // A cancelled order cannot be cancelled again; the domain rejects repeated cancellation.
        var repository = new InMemoryOrderRepository([order]);
        var handler = new CancelOrderHandler(repository);

        var result = await handler.HandleAsync(order.Id);

        Assert.Equal(CancelOrderOutcome.Conflict, result.Outcome);
        Assert.NotNull(result.Order);
        Assert.Equal(nameof(OrderStatus.Cancelled), result.Order.Status);
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
