namespace OrderManagement.Application.Orders;
public sealed record OrderLineItemDetails(Guid Id,string ProductSku,int Quantity,decimal UnitPrice,decimal LineTotal);