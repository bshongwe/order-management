using OrderManagement.Domain.Customers;
using Xunit;

namespace OrderManagement.UnitTests.Customers;

public sealed class CustomerTests
{
    [Theory]
    [InlineData("za", "ZA")]
    [InlineData("BW", "BW")]
    [InlineData("zw", "ZW")]
    public void Create_WithSupportedCountry_NormalizesValues(string countryCode, string expected)
    {
        var customer = Customer.Create(" Customer Name ", "customer@example.com", countryCode);
        Assert.Equal("Customer Name", customer.Name);
        Assert.Equal("customer@example.com", customer.Email);
        Assert.Equal(expected, customer.CountryCode);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("XX")]
    [InlineData("")]
    public void Create_WithUnsupportedCountry_Throws(string countryCode) =>
        Assert.Throws<ArgumentException>(() => Customer.Create("Customer", "customer@example.com", countryCode));

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("customer@")]
    public void Create_WithInvalidEmail_Throws(string email) =>
        Assert.Throws<ArgumentException>(() => Customer.Create("Customer", email, "ZA"));

    [Fact]
    public void Create_WithBlankName_Throws() =>
        Assert.Throws<ArgumentException>(() => Customer.Create(" ", "customer@example.com", "ZA"));
}
