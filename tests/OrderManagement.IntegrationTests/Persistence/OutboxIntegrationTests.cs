using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.IntegrationTests.Infrastructure;
using Xunit;
namespace OrderManagement.IntegrationTests.Persistence;
[Collection(SqlServerCollection.Name)]
public sealed class OutboxIntegrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task TransitionIntoProcessing_PersistsOutboxMessageInSameSave()
    {
        var order=Order.Create("OUTBOX-TEST");
        order.Submit(); order.Confirm();
        await using(var setup=fixture.CreateDbContext())
        {
            await setup.Orders.AddAsync(order);
            await setup.SaveChangesAsync();
        }
        await using(var update=fixture.CreateDbContext())
        {
            var persisted=await update.Orders.SingleAsync(x=>x.Id==order.Id);
            persisted.StartProcessing();
            await update.SaveChangesAsync();
            var message=await update.OutboxMessages.SingleAsync(x=>x.Payload.Contains(order.Id.ToString("D")));
            Assert.Equal("order.processing.requested",message.EventType);
            Assert.Contains(order.Id.ToString("D"),message.Payload);
            Assert.Null(message.PublishedAtUtc);
        }
    }
}