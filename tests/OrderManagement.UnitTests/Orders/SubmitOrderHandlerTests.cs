using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
using Xunit;

namespace OrderManagement.UnitTests.Orders;

public sealed class SubmitOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenOrderIsDraft_SubmitsAndPersistsOrder()
    {
        var order = Order.Create("CUSTOMER-100");
        var repository = new InMemoryOrderRepository([order]);
        var handler = new SubmitOrderHandler(repository);

        var result = await handler.HandleAsync(order.Id);

        Assert.Equal(SubmitOrderOutcome.Submitted, result.Outcome);
        Assert.NotNull(result.Order);
        Assert.Equal(nameof(OrderStatus.Submitted), result.Order.Status);
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var repository = new InMemoryOrderRepository([]);
        var handler = new SubmitOrderHandler(repository);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Equal(SubmitOrderOutcome.NotFound, result.Outcome);
        Assert.Null(result.Order);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderIsNotDraft_ReturnsConflictWithoutSaving()
    {
        var order = Order.Create("CUSTOMER-100");
        order.Submit();
        var repository = new InMemoryOrderRepository([order]);
        var handler = new SubmitOrderHandler(repository);

        var result = await handler.HandleAsync(order.Id);

        Assert.Equal(SubmitOrderOutcome.Conflict, result.Outcome);
        Assert.NotNull(result.Order);
        Assert.Equal(nameof(OrderStatus.Submitted), result.Order.Status);
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
