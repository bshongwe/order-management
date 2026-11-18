using OrderManagement.Application.Customers;
using OrderManagement.Domain.Orders;
namespace OrderManagement.Application.Orders;
public sealed class CreateOrderHandler(IOrderRepository orderRepository,ICustomerRepository customerRepository)
{
    public async Task<CreateOrderResult> HandleAsync(CreateOrderCommand command,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Items);
        if(command.Items.Any(item=>item is null)) throw new ArgumentException("Order line items cannot contain null entries.",nameof(command));
        var customer=await customerRepository.GetByIdAsync(command.CustomerId,cancellationToken);
        if(customer is null) throw new CustomerNotFoundException(command.CustomerId);
        var currency=command.CurrencyCode?.Trim().ToUpperInvariant();
        if(!SADCCurrencyPolicy.IsSupported(customer.CountryCode,currency??string.Empty))
            throw new UnsupportedOrderCurrencyException(customer.CountryCode,currency??string.Empty);
        var items=command.Items.Select(item=>OrderLineItem.Create(item.ProductSku,item.Quantity,item.UnitPrice)).ToArray();
        var order=Order.Create(customer.Id,customer.Id.ToString("D"),currency!,items);
        await orderRepository.AddAsync(order,cancellationToken);
        return new CreateOrderResult(order.Id,order.CustomerReference,order.Status.ToString(),order.CreatedAtUtc,order.CustomerId,order.CurrencyCode,order.TotalAmount);
    }
}