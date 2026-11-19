using LinkedInContentManager.Application.ExternalServices.CrewAi;
using LinkedInContentManager.Contracts.Common;
using LinkedInContentManager.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LinkedInContentManager.API.ExceptionHandling;

/// <summary>
/// The single place every unhandled exception from a controller/MediatR handler lands.
/// Maps known exception types to the right status code; everything else becomes a generic
/// 500 with a trace ID — the full exception is logged server-side, never serialized into the
/// response (no stack traces, no connection strings, no API keys).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        var (statusCode, message) = exception switch
        {
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            DomainException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message),
            CrewAiServiceException crewEx => (StatusCodes.Status502BadGateway, crewEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "An internal error occurred. Contact support with the trace ID below."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", traceId);
        else
            logger.LogWarning(exception, "Handled exception mapped to {StatusCode}. TraceId={TraceId}", statusCode, traceId);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(message, traceId), cancellationToken);

        return true;
    }
}
