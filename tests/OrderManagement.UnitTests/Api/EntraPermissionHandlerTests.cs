using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OrderManagement.Api.Authentication;
using Xunit;

namespace OrderManagement.UnitTests.Api;

public sealed class EntraPermissionHandlerTests
{
    [Theory]
    [InlineData("roles", "Orders.Read")]
    [InlineData(ClaimTypes.Role, "Orders.Read")]
    [InlineData("scp", "openid Orders.Read profile")]
    [InlineData("scope", "Orders.Admin Orders.Read")]
    public async Task AuthorizeAsync_GrantedRoleOrScope_Succeeds(string claimType, string value)
    {
        var identity = new ClaimsIdentity([new Claim(claimType, value)], "test");
        var context = new AuthorizationHandlerContext(
            [new EntraPermissionRequirement("Orders.Read")],
            new ClaimsPrincipal(identity),
            null);
        var handler = new EntraPermissionHandler();

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task AuthorizeAsync_MissingPermission_DoesNotSucceed()
    {
        var identity = new ClaimsIdentity([new Claim("roles", "Orders.Write")], "test");
        var context = new AuthorizationHandlerContext(
            [new EntraPermissionRequirement("Orders.Read")],
            new ClaimsPrincipal(identity),
            null);

        await new EntraPermissionHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task AuthorizeAsync_UnauthenticatedPrincipal_DoesNotSucceed()
    {
        var identity = new ClaimsIdentity([new Claim("roles", "Orders.Read")]);
        var context = new AuthorizationHandlerContext(
            [new EntraPermissionRequirement("Orders.Read")],
            new ClaimsPrincipal(identity),
            null);

        await new EntraPermissionHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
