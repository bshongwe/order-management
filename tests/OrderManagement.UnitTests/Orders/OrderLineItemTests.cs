using OrderManagement.Domain.Orders;
using Xunit;
namespace OrderManagement.UnitTests.Orders;
public sealed class OrderLineItemTests
{
 [Fact] public void Create_ValidLineItem_NormalizesSkuAndCalculatesLineTotal()
 { var item=OrderLineItem.Create(" SKU-1 ",3,12.50m); Assert.Equal("SKU-1",item.ProductSku); Assert.Equal(37.50m,item.LineTotal); }
 [Theory][InlineData("",1,10)][InlineData("SKU",0,10)][InlineData("SKU",-1,10)][InlineData("SKU",1,-1)][InlineData("SKU",1,1.001)]
 public void Create_InvalidLineItem_Throws(string sku,int quantity,decimal price)=>Assert.ThrowsAny<ArgumentException>(()=>OrderLineItem.Create(sku,quantity,price));
}