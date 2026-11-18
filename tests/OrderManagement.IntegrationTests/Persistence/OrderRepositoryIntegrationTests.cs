using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderManagement.IntegrationTests.Persistence;

[Collection(SqlServerCollection.Name)]
public sealed class OrderRepositoryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        await dbContext.Orders.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrateAsync_AppliesCurrentMigrationsAndConcurrencyMetadata()
    {
        await using var dbContext = fixture.CreateDbContext();

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Contains("20261118144526_InitialCreate", appliedMigrations);
        Assert.Contains("20261118203000_AddOrderRowVersion", appliedMigrations);
        Assert.Empty(pendingMigrations);

        var rowVersion = dbContext.Model.FindEntityType(typeof(Order))!
            .FindProperty("RowVersion");

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenOrderWasUpdatedByAnotherContext_ThrowsConcurrencyException()
    {
        var order = Order.Create("CUSTOMER-CONCURRENT");

        await using (var seedContext = fixture.CreateDbContext())
        {
            await new OrderRepository(seedContext).AddAsync(order, CancellationToken.None);
        }

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();

        var firstRepository = new OrderRepository(firstContext);
        var secondRepository = new OrderRepository(secondContext);

        var firstCopy = await firstRepository.GetByIdForUpdateAsync(order.Id, CancellationToken.None);
        var secondCopy = await secondRepository.GetByIdForUpdateAsync(order.Id, CancellationToken.None);

        Assert.NotNull(firstCopy);
        Assert.NotNull(secondCopy);

        firstCopy.Submit();
        await firstRepository.SaveChangesAsync(CancellationToken.None);

        secondCopy.Cancel();

        await Assert.ThrowsAsync<OrderConcurrencyException>(
            () => secondRepository.SaveChangesAsync(CancellationToken.None));

        await using var verificationContext = fixture.CreateDbContext();
        var persistedOrder = await verificationContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(OrderStatus.Submitted, persistedOrder.Status);
    }

    [Fact]
    public async Task AddAsync_PersistsOrderAndItsLifecycleStatus()
    {
        var order = Order.Create("CUSTOMER-100");
        order.Submit();

        await using (var writeContext = fixture.CreateDbContext())
        {
            var repository = new OrderRepository(writeContext);
            await repository.AddAsync(order, CancellationToken.None);
        }

        await using var readContext = fixture.CreateDbContext();
        var persistedOrder = await readContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal("CUSTOMER-100", persistedOrder.CustomerReference);
        Assert.Equal(OrderStatus.Submitted, persistedOrder.Status);
        Assert.Equal(order.CreatedAtUtc, persistedOrder.CreatedAtUtc);
    }

    [Fact]
    public async Task SubmitOrderHandler_WhenOrderIsDraft_PersistsSubmittedStatus()
    {
        var order = Order.Create("CUSTOMER-150");

        await using (var writeContext = fixture.CreateDbContext())
        {
            await new OrderRepository(writeContext).AddAsync(order, CancellationToken.None);
        }

        await using (var updateContext = fixture.CreateDbContext())
        {
            var handler = new SubmitOrderHandler(new OrderRepository(updateContext));
            var result = await handler.HandleAsync(order.Id, CancellationToken.None);

            Assert.Equal(SubmitOrderOutcome.Submitted, result.Outcome);
            Assert.Equal(nameof(OrderStatus.Submitted), result.Order!.Status);
        }

        await using var readContext = fixture.CreateDbContext();
        var persistedOrder = await readContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(OrderStatus.Submitted, persistedOrder.Status);
    }

    [Fact]
    public async Task ConfirmOrderHandler_WhenOrderIsSubmitted_PersistsConfirmedStatus()
    {
        var order = Order.Create("CUSTOMER-160");
        order.Submit();

        await using (var writeContext = fixture.CreateDbContext())
        {
            await new OrderRepository(writeContext).AddAsync(order, CancellationToken.None);
        }

        await using (var updateContext = fixture.CreateDbContext())
        {
            var handler = new ConfirmOrderHandler(new OrderRepository(updateContext));
            var result = await handler.HandleAsync(order.Id, CancellationToken.None);

            Assert.Equal(ConfirmOrderOutcome.Confirmed, result.Outcome);
            Assert.Equal(nameof(OrderStatus.Confirmed), result.Order!.Status);
        }

        await using var readContext = fixture.CreateDbContext();
        var persistedOrder = await readContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(OrderStatus.Confirmed, persistedOrder.Status);
    }

    [Fact]
    public async Task ProcessOrderHandler_WhenOrderIsConfirmed_PersistsProcessingStatus()
    {
        var order = Order.Create("CUSTOMER-170");
        order.Submit();
        order.Confirm();

        await using (var writeContext = fixture.CreateDbContext())
        {
            await new OrderRepository(writeContext).AddAsync(order, CancellationToken.None);
        }

        await using (var updateContext = fixture.CreateDbContext())
        {
            var handler = new ProcessOrderHandler(new OrderRepository(updateContext));
            var result = await handler.HandleAsync(order.Id, CancellationToken.None);

            Assert.Equal(ProcessOrderOutcome.ProcessingStarted, result.Outcome);
            Assert.Equal(nameof(OrderStatus.Processing), result.Order!.Status);
        }

        await using var readContext = fixture.CreateDbContext();
        var persistedOrder = await readContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(OrderStatus.Processing, persistedOrder.Status);
    }

    [Fact]
    public async Task FulfillOrderHandler_WhenOrderIsProcessing_PersistsFulfilledStatus()
    {
        var order = Order.Create("CUSTOMER-180");
        order.Submit();
        order.Confirm();
        order.StartProcessing();

        await using (var writeContext = fixture.CreateDbContext())
        {
            await new OrderRepository(writeContext).AddAsync(order, CancellationToken.None);
        }

        await using (var updateContext = fixture.CreateDbContext())
        {
            var handler = new FulfillOrderHandler(new OrderRepository(updateContext));
            var result = await handler.HandleAsync(order.Id, CancellationToken.None);

            Assert.Equal(FulfillOrderOutcome.Fulfilled, result.Outcome);
            Assert.Equal(nameof(OrderStatus.Fulfilled), result.Order!.Status);
        }

        await using var readContext = fixture.CreateDbContext();
        var persistedOrder = await readContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(OrderStatus.Fulfilled, persistedOrder.Status);
    }

    [Fact]
    public async Task CancelOrderHandler_WhenOrderIsSubmitted_PersistsCancelledStatus()
    {
        var order = Order.Create("CUSTOMER-175");
        order.Submit();

        await using (var writeContext = fixture.CreateDbContext())
        {
            await new OrderRepository(writeContext).AddAsync(order, CancellationToken.None);
        }

        await using (var updateContext = fixture.CreateDbContext())
        {
            var handler = new CancelOrderHandler(new OrderRepository(updateContext));
            var result = await handler.HandleAsync(order.Id, CancellationToken.None);

            Assert.Equal(CancelOrderOutcome.Cancelled, result.Outcome);
            Assert.Equal(nameof(OrderStatus.Cancelled), result.Order!.Status);
        }

        await using var readContext = fixture.CreateDbContext();
        var persistedOrder = await readContext.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(OrderStatus.Cancelled, persistedOrder.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProjectedOrderDetails()
    {
        var order = Order.Create("CUSTOMER-200");

        await using (var writeContext = fixture.CreateDbContext())
        {
            await new OrderRepository(writeContext).AddAsync(order, CancellationToken.None);
        }

        await using var readContext = fixture.CreateDbContext();
        var result = await new OrderRepository(readContext)
            .GetByIdAsync(order.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("CUSTOMER-200", result.CustomerReference);
        Assert.Equal(nameof(OrderStatus.Draft), result.Status);
        Assert.Equal(order.CreatedAtUtc, result.CreatedAtUtc);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        await using var dbContext = fixture.CreateDbContext();
        var result = await new OrderRepository(dbContext)
            .GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsSqlServerPagedResultsAndTotalCount()
    {
        var orders = Enumerable.Range(1, 3)
            .Select(index => Order.Create($"CUSTOMER-{index}"))
            .ToArray();

        await using (var writeContext = fixture.CreateDbContext())
        {
            var repository = new OrderRepository(writeContext);

            foreach (var order in orders)
            {
                await repository.AddAsync(order, CancellationToken.None);
            }
        }

        await using var readContext = fixture.CreateDbContext();
        var repositoryForRead = new OrderRepository(readContext);

        var firstPage = await repositoryForRead.GetPageAsync(1, 2, CancellationToken.None);
        var secondPage = await repositoryForRead.GetPageAsync(2, 2, CancellationToken.None);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Single(secondPage.Items);
        Assert.Equal(3, firstPage.Items.Count + secondPage.Items.Count);
        Assert.All(firstPage.Items.Concat(secondPage.Items), item =>
            Assert.Equal(nameof(OrderStatus.Draft), item.Status));
    }
}
