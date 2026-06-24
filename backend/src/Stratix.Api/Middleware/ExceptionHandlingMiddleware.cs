using System.Text.Json;

namespace Stratix.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = "about:blank",
            title = detail,
            status,
            detail
        }));
    }
}
