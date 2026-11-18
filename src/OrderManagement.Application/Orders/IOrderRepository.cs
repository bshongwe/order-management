using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task<OrderDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<OrderPage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
}
