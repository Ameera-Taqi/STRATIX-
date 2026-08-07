using System.Diagnostics;
using Stratix.Api.Observability;

namespace Stratix.Api.Middleware;

/// <summary>
/// Assigns a correlation id, measures duration, and emits a structured request-completed log.
/// Does not log bodies, Authorization headers, passwords, or tokens.
/// </summary>
public sealed class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTelemetryMiddleware> _logger;

    public RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationId.GetOrCreate(context);
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationId.HeaderName))
                context.Response.Headers[CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;
        var sw = Stopwatch.StartNew();

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestMethod"] = method,
            ["RequestPath"] = SanitizePath(path)
        }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var status = context.Response.StatusCode;
                var level = status >= 500 ? LogLevel.Error
                    : status >= 400 ? LogLevel.Warning
                    : LogLevel.Information;

                _logger.Log(
                    level,
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    method,
                    SanitizePath(path),
                    status,
                    sw.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>Strip query strings — they may contain reset tokens.</summary>
    private static string SanitizePath(string path)
    {
        var q = path.IndexOf('?', StringComparison.Ordinal);
        return q >= 0 ? path[..q] : path;
    }
}
