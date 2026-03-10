using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Common.Exceptions;

/// <summary>
/// Handles <see cref="ValidationException"/> by returning ProblemDetails with validation errors.
/// </summary>
/// <remarks>
/// Maps validation errors to HTTP 400 responses and includes the error dictionary in extensions.
/// </remarks>
internal sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ValidationExceptionHandler> logger)
    : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle a validation exception.
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <param name="exception">Exception to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the response was written; otherwise <c>false</c>.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetails = new ProblemDetails
        {
            Type = validationException.GetType().Name,
            Title = "Validation failed",
            Detail = validationException.Message,
            Status = StatusCodes.Status400BadRequest,
            Extensions =
            {
                ["errors"] = validationException.Errors
            }
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
