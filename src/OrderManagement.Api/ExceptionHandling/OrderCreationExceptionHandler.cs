using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Orders;
namespace OrderManagement.Api.ExceptionHandling;
public sealed class OrderCreationExceptionHandler(ILogger<OrderCreationExceptionHandler> logger):IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,Exception exception,CancellationToken cancellationToken)
    {
        var (status,title)=exception switch
        {
            CustomerNotFoundException=>(StatusCodes.Status404NotFound,"Customer not found"),
            UnsupportedOrderCurrencyException=>(StatusCodes.Status422UnprocessableEntity,"Unsupported currency"),
            ArgumentException=>(StatusCodes.Status400BadRequest,"Invalid order"),
            _=>(0,string.Empty)
        };
        if(status==0) return false;
        logger.LogInformation(exception,"Order creation rejected with status {StatusCode}",status);
        httpContext.Response.StatusCode=status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails { Status=status,Title=title,Detail=exception.Message },cancellationToken);
        return true;
    }
}