namespace OrderManagement.Application.Orders;
public sealed record CreateOrderLineItemCommand(string ProductSku,int Quantity,decimal UnitPrice);
public sealed record CreateOrderCommand(Guid CustomerId,string CurrencyCode,IReadOnlyList<CreateOrderLineItemCommand> Items);