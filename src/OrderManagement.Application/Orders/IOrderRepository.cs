using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
