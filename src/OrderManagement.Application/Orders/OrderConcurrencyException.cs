namespace OrderManagement.Application.Orders;

/// <summary>
/// Indicates that an order changed after it was loaded for an update.
/// </summary>
public sealed class OrderConcurrencyException(string message, Exception innerException)
    : Exception(message, innerException);
