namespace OrderManagement.Domain.Orders;

public enum OrderStatus
{
    Draft = 0,
    Submitted = 1,
    Confirmed = 2,
    Processing = 3,
    Fulfilled = 4,
    Cancelled = 5
}
