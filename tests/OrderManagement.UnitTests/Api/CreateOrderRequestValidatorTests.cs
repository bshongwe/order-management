using OrderManagement.Api.Contracts;
using Xunit;

namespace OrderManagement.UnitTests.Api;

public sealed class CreateOrderRequestValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), "zar",
            [new CreateOrderLineItemRequest("SKU-1", 2, 10.25m)]);

        Assert.Empty(CreateOrderRequestValidator.Validate(request));
    }

    [Fact]
    public void Validate_MissingItems_ReturnsValidationError()
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), "ZAR", null!);

        var errors = CreateOrderRequestValidator.Validate(request);

        Assert.Contains(nameof(CreateOrderRequest.Items), errors.Keys);
    }

    [Fact]
    public void Validate_EmptyCustomerAndInvalidLineItem_ReturnsFieldErrors()
    {
        var request = new CreateOrderRequest(Guid.Empty, "US",
            [new CreateOrderLineItemRequest(" ", 0, 1.001m)]);

        var errors = CreateOrderRequestValidator.Validate(request);

        Assert.Contains(nameof(CreateOrderRequest.CustomerId), errors.Keys);
        Assert.Contains(nameof(CreateOrderRequest.CurrencyCode), errors.Keys);
        Assert.Contains("items[0].productSku", errors.Keys);
        Assert.Contains("items[0].quantity", errors.Keys);
        Assert.Contains("items[0].unitPrice", errors.Keys);
    }

    [Fact]
    public void Validate_NullRequest_ReturnsRequestError()
    {
        var errors = CreateOrderRequestValidator.Validate(null);

        Assert.Contains("request", errors.Keys);
    }
}
