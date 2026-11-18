using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OrderRepository(OrdersDbContext dbContext) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<OrderDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new OrderDetails(
                order.Id,
                order.CustomerReference,
                order.Status.ToString(),
                order.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<OrderPage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await dbContext.Orders.CountAsync(cancellationToken);

        var items = await dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new OrderSummary(
                order.Id,
                order.CustomerReference,
                order.Status.ToString(),
                order.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new OrderPage(items, page, pageSize, totalCount, totalPages);
    }
}
