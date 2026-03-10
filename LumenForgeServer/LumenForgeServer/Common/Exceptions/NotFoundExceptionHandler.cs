using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Common.Exceptions
{
    /// <summary>
    /// Handles <see cref="NotFoundException"/> by returning a 404 ProblemDetails response.
    /// </summary>
    /// <remarks>
    /// Maps unique constraint violations to HTTP 404 responses.
    /// </remarks>
    internal sealed class NotFoundExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<NotFoundExceptionHandler> logger) : IExceptionHandler
    {
        /// <summary>
        /// Attempts to handle not found exception.
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
            if (exception is not NotFoundException)
            {
                return false;
            }

            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            httpContext.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Type = "https://httpstatuses.com/404",
                Title = "Did not find ressource requested",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound,
            };

            var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            });

            logger.LogInformation("After write: written={Written} Status={Status} Started={Started}",
                written,
                httpContext.Response.StatusCode,
                httpContext.Response.HasStarted);

            return written;
        }
    }
}