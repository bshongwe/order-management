using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
using Xunit;

namespace OrderManagement.UnitTests.Orders;

public sealed class OrderRetrievalHandlerTests
{
    [Fact]
    public async Task GetOrderById_WithExistingOrder_ReturnsOrderDetails()
    {
        var order = Order.Create("CUSTOMER-42");
        var repository = new InMemoryOrderRepository([order]);
        var handler = new GetOrderByIdHandler(repository);

        var result = await handler.HandleAsync(new GetOrderByIdQuery(order.Id));

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("CUSTOMER-42", result.CustomerReference);
        Assert.Equal(nameof(OrderStatus.Draft), result.Status);
    }

    [Fact]
    public async Task GetOrderById_WithUnknownId_ReturnsNull()
    {
        var handler = new GetOrderByIdHandler(new InMemoryOrderRepository([]));

        var result = await handler.HandleAsync(new GetOrderByIdQuery(Guid.NewGuid()));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrders_WithValidQuery_ReturnsRequestedPage()
    {
        var orders = Enumerable.Range(0, 3)
            .Select(index => Order.Create($"CUSTOMER-{index}"))
            .ToArray();
        var handler = new GetOrdersHandler(new InMemoryOrderRepository(orders));

        var result = await handler.HandleAsync(new GetOrdersQuery(Page: 2, PageSize: 2));

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task GetOrders_WithInvalidPagination_Throws(int page, int pageSize)
    {
        var handler = new GetOrdersHandler(new InMemoryOrderRepository([]));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => handler.HandleAsync(new GetOrdersQuery(page, pageSize)));
    }

    private sealed class InMemoryOrderRepository(IReadOnlyList<Order> orders) : IOrderRepository
    {
        public Task AddAsync(Order order, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(orders.SingleOrDefault(candidate => candidate.Id == id));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<OrderDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var order = orders.SingleOrDefault(candidate => candidate.Id == id);
            return Task.FromResult(order is null
                ? null
                : new OrderDetails(order.Id, order.CustomerReference, order.Status.ToString(), order.CreatedAtUtc));
        }

        public Task<OrderPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var ordered = orders
                .OrderByDescending(order => order.CreatedAtUtc)
                .ThenBy(order => order.Id)
                .ToArray();
            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(order => new OrderSummary(
                    order.Id,
                    order.CustomerReference,
                    order.Status.ToString(),
                    order.CreatedAtUtc))
                .ToArray();
            var totalPages = ordered.Length == 0
                ? 0
                : (int)Math.Ceiling(ordered.Length / (double)pageSize);

            return Task.FromResult(new OrderPage(items, page, pageSize, ordered.Length, totalPages));
        }
    }
}
