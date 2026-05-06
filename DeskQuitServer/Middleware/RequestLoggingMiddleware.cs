using System.Diagnostics;

namespace DeskQuitServer.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var traceId = context.TraceIdentifier;

        try
        {
            await _next(context);

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms ({TraceId})",
                method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                traceId);
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            _logger.LogError(
                ex,
                "HTTP {Method} {Path} failed with {StatusCode} in {ElapsedMs} ms ({TraceId})",
                method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                traceId);

            throw;
        }
    }
}

