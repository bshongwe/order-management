using System.Diagnostics;
using System.Diagnostics.Metrics;
namespace OrderManagement.Api.Observability;
public static class ApiMetrics
{
    public static readonly Meter Meter = new("OrderManagement.Api", "1.0.0");
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>("http.server.requests");
    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("http.server.request.duration", "ms");
}
public sealed class RequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try { await next(context); }
        finally
        {
            var tags = new TagList
            {
                { "http.request.method", context.Request.Method },
                { "http.response.status_code", context.Response.StatusCode }
            };
            ApiMetrics.Requests.Add(1, tags);
            ApiMetrics.Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, tags);
        }
    }
}