namespace OrderManagement.Api.Contracts;
public sealed record CreateOrderLineItemRequest(string ProductSku,int Quantity,decimal UnitPrice);
public sealed record CreateOrderRequest(Guid CustomerId,string CurrencyCode,IReadOnlyList<CreateOrderLineItemRequest> Items);