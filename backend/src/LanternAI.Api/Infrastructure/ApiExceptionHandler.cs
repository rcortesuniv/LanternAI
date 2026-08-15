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

        // Sanitize the request path to prevent log-forging — strip control
        // characters and newlines that could inject fake log entries.
        var sanitizedPath = SanitizeLogInput(httpContext.Request.Path.Value);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Path}", sanitizedPath);
        }
        else
        {
            logger.LogWarning(exception, "{Title} on {Path}", title, sanitizedPath);
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

    /// <summary>Removes control characters (including CR/LF) from user-supplied values before logging.</summary>
    private static string SanitizeLogInput(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (!char.IsControl(c))
                sb.Append(c);
        }
        return sb.ToString();
    }
}
