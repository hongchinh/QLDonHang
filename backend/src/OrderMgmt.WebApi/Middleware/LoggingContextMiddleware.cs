using System.Security.Claims;
using Serilog.Context;

namespace OrderMgmt.WebApi.Middleware;

public class LoggingContextMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var headerValue)
                            && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : context.TraceIdentifier;

        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User?.FindFirst("sub")?.Value;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        {
            await _next(context);
        }
    }
}
