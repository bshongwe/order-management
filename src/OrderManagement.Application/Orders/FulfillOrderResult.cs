namespace OrderManagement.Application.Orders;

public sealed record FulfillOrderResult(
    FulfillOrderOutcome Outcome,
    OrderDetails? Order);
