namespace OrderManagement.Application.Orders;

public sealed record ConfirmOrderResult(
    ConfirmOrderOutcome Outcome,
    OrderDetails? Order);
