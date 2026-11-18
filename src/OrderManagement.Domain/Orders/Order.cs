namespace OrderManagement.Domain.Orders;
public sealed class Order
{
    private readonly List<OrderLineItem> _lineItems=[];
    private Order() { }
    private Order(Guid id,string customerReference) { Id=id; CustomerReference=customerReference; Status=OrderStatus.Draft; CreatedAtUtc=DateTimeOffset.UtcNow; }
    public Guid Id { get; private set; }
    public string CustomerReference { get; private set; }=string.Empty;
    public Guid? CustomerId { get; private set; }
    public string? CurrencyCode { get; private set; }
    public decimal? TotalAmount { get; private set; }
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    // Retained for historical orders that predate customer management.
    public static Order Create(string customerReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerReference);
        var reference=customerReference.Trim();
        if(reference.Length>64) throw new ArgumentOutOfRangeException(nameof(customerReference),"Customer reference cannot exceed 64 characters.");
        return new Order(Guid.NewGuid(),reference);
    }
    public static Order Create(Guid customerId,string customerReference,string currencyCode,IEnumerable<OrderLineItem> lineItems)
    {
        if(customerId==Guid.Empty) throw new ArgumentException("A customer ID is required.",nameof(customerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(customerReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentNullException.ThrowIfNull(lineItems);
        var reference=customerReference.Trim();
        if(reference.Length>64) throw new ArgumentOutOfRangeException(nameof(customerReference),"Customer reference cannot exceed 64 characters.");
        var currency=currencyCode.Trim().ToUpperInvariant();
        if(currency.Length!=3 || !currency.All(char.IsAsciiLetter)) throw new ArgumentException("Currency code must be a three-letter ISO code.",nameof(currencyCode));
        var items=lineItems.ToArray();
        if(items.Length==0) throw new ArgumentException("An order must contain at least one line item.",nameof(lineItems));
        if(items.Any(item=>item is null)) throw new ArgumentException("Order line items cannot contain null entries.",nameof(lineItems));
        decimal total=0;
        try { foreach(var item in items) total=checked(total+item.LineTotal); }
        catch(OverflowException exception) { throw new ArgumentException("Order total exceeds the supported monetary range.",nameof(lineItems),exception); }
        if(total>9_999_999_999_999_999.99m) throw new ArgumentOutOfRangeException(nameof(lineItems),"Order total exceeds decimal(18,2).");
        var order=new Order(Guid.NewGuid(),reference) { CustomerId=customerId,CurrencyCode=currency,TotalAmount=total };
        order._lineItems.AddRange(items);
        return order;
    }
    public void Submit() { if(Status!=OrderStatus.Draft) throw new InvalidOperationException("Only draft orders can be submitted."); Status=OrderStatus.Submitted; }
    public void Confirm() { if(Status!=OrderStatus.Submitted) throw new InvalidOperationException("Only submitted orders can be confirmed."); Status=OrderStatus.Confirmed; }
    public void StartProcessing() { if(Status!=OrderStatus.Confirmed) throw new InvalidOperationException("Only confirmed orders can start processing."); Status=OrderStatus.Processing; }
    public void Fulfill() { if(Status!=OrderStatus.Processing) throw new InvalidOperationException("Only orders in processing can be fulfilled."); Status=OrderStatus.Fulfilled; }
    public void Cancel() { if(Status is OrderStatus.Fulfilled or OrderStatus.Cancelled) throw new InvalidOperationException($"An order in status '{Status}' cannot be cancelled."); Status=OrderStatus.Cancelled; }
}