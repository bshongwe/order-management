using System.Diagnostics;
namespace OrderManagement.Api.Observability;
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-ID";
    public async Task InvokeAsync(HttpContext context)
    {
        var id = Guid.TryParse(context.Request.Headers[HeaderName].FirstOrDefault(), out var supplied)
            ? supplied.ToString("D") : Guid.NewGuid().ToString("D");
        context.TraceIdentifier = id;
        context.Response.Headers[HeaderName] = id;
        Activity.Current?.SetTag("correlation.id", id);
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            await next(context);
    }
}