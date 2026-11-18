using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace OrderManagement.Api.Authentication;

public sealed class EntraPermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class EntraPermissionHandler : AuthorizationHandler<EntraPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EntraPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var hasAppRole = context.User.Claims.Any(claim =>
            (claim.Type == "roles" || claim.Type == ClaimTypes.Role) &&
            string.Equals(claim.Value, requirement.Permission, StringComparison.Ordinal));

        var hasDelegatedScope = context.User.Claims
            .Where(claim => claim.Type is "scp" or "scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(requirement.Permission, StringComparer.Ordinal);

        if (hasAppRole || hasDelegatedScope)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
