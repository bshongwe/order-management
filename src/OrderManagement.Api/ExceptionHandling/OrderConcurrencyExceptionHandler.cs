using Microsoft.AspNetCore.Diagnostics;
using OrderManagement.Application.Orders;

namespace OrderManagement.Api.ExceptionHandling;

public sealed class OrderConcurrencyExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not OrderConcurrencyException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Order was modified by another operation",
            detail: "Reload the order and retry the operation.").ExecuteAsync(httpContext);

        return true;
    }
}
