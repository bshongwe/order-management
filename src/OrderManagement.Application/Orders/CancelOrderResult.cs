namespace OrderManagement.Application.Orders;

public sealed record CancelOrderResult(
    CancelOrderOutcome Outcome,
    OrderDetails? Order);
