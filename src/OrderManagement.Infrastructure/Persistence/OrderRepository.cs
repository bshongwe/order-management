using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
namespace OrderManagement.Infrastructure.Persistence;
public sealed class OrderRepository(OrdersDbContext dbContext):IOrderRepository
{
    public async Task AddAsync(Order order,CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order,cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    public Task<OrderDetails?> GetByIdAsync(Guid id,CancellationToken cancellationToken)=>
        dbContext.Orders.AsNoTracking().Where(x=>x.Id==id)
            .Select(x=>new OrderDetails(x.Id,x.CustomerReference,x.Status.ToString(),x.CreatedAtUtc,x.CustomerId,x.CurrencyCode,x.TotalAmount,
                x.LineItems.Select(i=>new OrderLineItemDetails(i.Id,i.ProductSku,i.Quantity,i.UnitPrice,i.Quantity*i.UnitPrice)).ToList(), EF.Property<byte[]>(x, "RowVersion")))
            .SingleOrDefaultAsync(cancellationToken);
    public Task<Order?> GetByIdForUpdateAsync(Guid id,CancellationToken cancellationToken)=>
        dbContext.Orders.Include(x=>x.LineItems).SingleOrDefaultAsync(x=>x.Id==id,cancellationToken);
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch(DbUpdateConcurrencyException exception) { throw new OrderConcurrencyException("The order was modified by another operation.",exception); }
    }
    public async Task<OrderPage> GetPageAsync(int page,int pageSize,CancellationToken cancellationToken)
    {
        var totalCount=await dbContext.Orders.CountAsync(cancellationToken);
        var items=await dbContext.Orders.AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).ThenBy(x=>x.Id)
            .Skip((page-1)*pageSize).Take(pageSize)
            .Select(x=>new OrderSummary(x.Id,x.CustomerReference,x.Status.ToString(),x.CreatedAtUtc,x.CustomerId,x.CurrencyCode,x.TotalAmount))
            .ToListAsync(cancellationToken);
        var totalPages=totalCount==0?0:(int)Math.Ceiling(totalCount/(double)pageSize);
        return new OrderPage(items,page,pageSize,totalCount,totalPages);
    }
}