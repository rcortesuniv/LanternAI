using LanternAI.Api.Services.Llm;
using LanternAI.Api.Services.QueryPlanning;
using Microsoft.AspNetCore.Diagnostics;

namespace LanternAI.Api.Infrastructure;

/// <summary>
/// Central error boundary: maps known exception types to clean ProblemDetails
/// responses and logs everything else without ever leaking stack traces or
/// internal details to the client.
/// </summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            InvalidQueryPlanException ex => (StatusCodes.Status400BadRequest, "Could not understand that question", ex.Message),
            LlmUnavailableException ex => (StatusCodes.Status503ServiceUnavailable, "Language model unavailable", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred. Please try again."),
        };
        var requestPathForLog = httpContext.Request.Path.ToString()
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Path}", requestPathForLog);
        }
        else
        {
            logger.LogWarning(exception, "{Title} on {Path}", title, requestPathForLog);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail,
            correlationId = httpContext.TraceIdentifier,
        }, cancellationToken);

        return true;
    }
}
