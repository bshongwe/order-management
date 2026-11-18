using OrderManagement.Domain.Orders;
using Xunit;
namespace OrderManagement.UnitTests.Orders;
public sealed class SADCCurrencyPolicyTests
{
 [Theory]
 [InlineData("ZA","ZAR")][InlineData("NA","NAD")][InlineData("NA","ZAR")][InlineData("LS","ZAR")][InlineData("SZ","ZAR")][InlineData("ZW","ZWL")][InlineData("ZW","USD")]
 public void IsSupported_WithAllowedPair_ReturnsTrue(string country,string currency)=>Assert.True(SADCCurrencyPolicy.IsSupported(country,currency));
 [Theory]
 [InlineData("ZA","USD")][InlineData("BW","ZAR")][InlineData("ZW","ZAR")][InlineData("XX","ZAR")]
 public void IsSupported_WithUnsupportedPair_ReturnsFalse(string country,string currency)=>Assert.False(SADCCurrencyPolicy.IsSupported(country,currency));
}