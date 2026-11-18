using OrderManagement.Domain.Orders;

namespace OrderManagement.UnitTests.Orders;

public sealed class OrderTests
{
    [Fact]
    public void Create_WithValidCustomerReference_CreatesDraftOrder()
    {
        var order = Order.Create("  CUSTOMER-123  ");

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("CUSTOMER-123", order.CustomerReference);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Create_WithBlankCustomerReference_Throws()
    {
        Assert.Throws<ArgumentException>(() => Order.Create("  "));
    }

    [Fact]
    public void Submit_WhenDraft_TransitionsToSubmitted()
    {
        var order = Order.Create("CUSTOMER-123");

        order.Submit();

        Assert.Equal(OrderStatus.Submitted, order.Status);
    }

    [Fact]
    public void Submit_WhenAlreadySubmitted_Throws()
    {
        var order = Order.Create("CUSTOMER-123");
        order.Submit();

        Assert.Throws<InvalidOperationException>(order.Submit);
    }

    [Fact]
    public void Cancel_WhenFulfilled_Throws()
    {
        var order = Order.Create("CUSTOMER-123");
        order.Submit();
        SetStatus(order, OrderStatus.Fulfilled);

        Assert.Throws<InvalidOperationException>(order.Cancel);
    }

    private static void SetStatus(Order order, OrderStatus status)
    {
        var property = typeof(Order).GetProperty(nameof(Order.Status))!;
        property.SetValue(order, status);
    }
}
