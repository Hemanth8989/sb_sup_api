using System.Diagnostics;

namespace StoneBridge.Api.Middleware;

/// <summary>
/// Logs every HTTP request with method, path, status code, and elapsed time.
/// Assigns a correlation ID from the X-Correlation-Id request header or auto-generates one.
/// Returns the correlation ID in the response header for client-side debugging.
/// Log levels: Information (normal), Warning (> 500ms), Error (> 2s).
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate                  _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate                   next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Assign or forward correlation ID
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                         ?? context.TraceIdentifier;

        context.Response.Headers["X-Correlation-Id"] = correlationId;

        using var scope = _logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var elapsedMs  = sw.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;
            var method     = context.Request.Method;
            var path       = context.Request.Path.Value ?? "/";

            var logLevel = elapsedMs switch
            {
                >= 2_000 => LogLevel.Error,
                >= 500   => LogLevel.Warning,
                _        => LogLevel.Information,
            };

            _logger.Log(
                logLevel,
                "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                method, path, statusCode, elapsedMs, correlationId);
        }
    }
}