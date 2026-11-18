namespace OrderManagement.Application.Orders;

public sealed record SubmitOrderResult(
    SubmitOrderOutcome Outcome,
    OrderDetails? Order);
