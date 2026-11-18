using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
using Xunit;

namespace OrderManagement.UnitTests.Orders;

public sealed class CreateOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_PersistsAndReturnsOrder()
    {
        var repository = new InMemoryOrderRepository();
        var handler = new CreateOrderHandler(repository);

        var result = await handler.HandleAsync(new CreateOrderCommand(" CUSTOMER-42 "));

        var persistedOrder = Assert.Single(repository.Orders);
        Assert.Equal(persistedOrder.Id, result.Id);
        Assert.Equal("CUSTOMER-42", result.CustomerReference);
        Assert.Equal(nameof(OrderStatus.Draft), result.Status);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCustomerReference_DoesNotPersist()
    {
        var repository = new InMemoryOrderRepository();
        var handler = new CreateOrderHandler(repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(new CreateOrderCommand(" ")));

        Assert.Empty(repository.Orders);
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        public List<Order> Orders { get; } = [];

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }
    }
}
