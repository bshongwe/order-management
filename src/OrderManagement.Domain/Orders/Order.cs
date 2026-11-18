namespace OrderManagement.Domain.Orders;

/// <summary>
/// Represents an order and protects its lifecycle invariants.
/// Persistence concerns deliberately remain outside the domain model.
/// </summary>
public sealed class Order
{
    private Order()
    {
        // Required by persistence materialization.
    }

    private Order(Guid id, string customerReference)
    {
        Id = id;
        CustomerReference = customerReference;
        Status = OrderStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string CustomerReference { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Order Create(string customerReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerReference);

        var normalizedReference = customerReference.Trim();

        if (normalizedReference.Length > 64)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customerReference),
                "Customer reference cannot exceed 64 characters.");
        }

        return new Order(Guid.NewGuid(), normalizedReference);
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be submitted.");
        }

        Status = OrderStatus.Submitted;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted orders can be confirmed.");
        }

        Status = OrderStatus.Confirmed;
    }

    public void StartProcessing()
    {
        if (Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed orders can start processing.");
        }

        Status = OrderStatus.Processing;
    }

    public void Fulfill()
    {
        if (Status != OrderStatus.Processing)
        {
            throw new InvalidOperationException("Only orders in processing can be fulfilled.");
        }

        Status = OrderStatus.Fulfilled;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Fulfilled or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"An order in status '{Status}' cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }
}
