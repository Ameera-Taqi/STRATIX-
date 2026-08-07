using System.Text.Json;
using FluentValidation;

namespace Stratix.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for {Method} {Path}", context.Request.Method, context.Request.Path);
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "One or more validation errors occurred.", errors);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized: {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (Stratix.Application.Services.PlanLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Plan limit exceeded: {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status402PaymentRequired, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict: {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string detail,
        IDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "about:blank",
            ["title"] = detail,
            ["status"] = status,
            ["detail"] = detail
        };
        if (errors is { Count: > 0 })
            payload["errors"] = errors;

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
