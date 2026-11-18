namespace OrderManagement.Application.Orders;

public sealed record ProcessOrderResult(
    ProcessOrderOutcome Outcome,
    OrderDetails? Order);
