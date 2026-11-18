using OrderManagement.Application.Orders;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderManagement.IntegrationTests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class OrderCreationIntegrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task CreateOrder_PersistsCustomerRelationshipLineItemsAndCalculatedTotal()
    {
        var customer = Customer.Create("Order Test", $"order-{Guid.NewGuid():N}@example.com", "ZA");
        Guid orderId;
        await using (var writeContext = fixture.CreateDbContext())
        {
            await new CustomerRepository(writeContext).AddAsync(customer, CancellationToken.None);
            var handler = new CreateOrderHandler(
                new OrderRepository(writeContext),
                new CustomerRepository(writeContext));
            var result = await handler.HandleAsync(new CreateOrderCommand(customer.Id, "ZAR",
            [
                new CreateOrderLineItemCommand("SKU-100", 2, 125.50m),
                new CreateOrderLineItemCommand("SKU-200", 1, 49.00m)
            ]));
            orderId = result.Id;
            Assert.Equal(300m, result.TotalAmount);
        }

        await using var readContext = fixture.CreateDbContext();
        var order = await new OrderRepository(readContext).GetByIdAsync(orderId, CancellationToken.None);
        Assert.NotNull(order);
        Assert.Equal(customer.Id, order.CustomerId);
        Assert.Equal("ZAR", order.CurrencyCode);
        Assert.Equal(300m, order.TotalAmount);
        Assert.Equal(2, order.LineItems!.Count);
        Assert.Equal(251m, order.LineItems.Single(item => item.ProductSku == "SKU-100").LineTotal);
    }
}
