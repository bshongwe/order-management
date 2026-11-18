using OrderManagement.Domain.Orders;
using Xunit;

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
    public void Cancel_WhenSubmitted_TransitionsToCancelled()
    {
        var order = Order.Create("CUSTOMER-123");
        order.Submit();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var order = Order.Create("CUSTOMER-123");
        order.Cancel();

        Assert.Throws<InvalidOperationException>(order.Cancel);
    }
}
