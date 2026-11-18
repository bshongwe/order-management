namespace OrderManagement.Domain.Orders;
public sealed class OrderLineItem
{
    private OrderLineItem() { }
    private OrderLineItem(Guid id, string productSku, int quantity, decimal unitPrice) { Id=id; ProductSku=productSku; Quantity=quantity; UnitPrice=unitPrice; }
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public static OrderLineItem Create(string productSku, int quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productSku);
        var sku=productSku.Trim();
        if(sku.Length>64) throw new ArgumentOutOfRangeException(nameof(productSku),"Product SKU cannot exceed 64 characters.");
        if(quantity<=0) throw new ArgumentOutOfRangeException(nameof(quantity),"Quantity must be greater than zero.");
        if(unitPrice<0 || unitPrice>9_999_999_999_999_999.99m || decimal.Round(unitPrice,2)!=unitPrice)
            throw new ArgumentOutOfRangeException(nameof(unitPrice),"Unit price must fit decimal(18,2) and have at most two decimal places.");
        return new OrderLineItem(Guid.NewGuid(),sku,quantity,unitPrice);
    }
}