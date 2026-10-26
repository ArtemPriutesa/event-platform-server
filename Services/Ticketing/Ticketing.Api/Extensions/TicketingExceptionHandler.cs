using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Api.Extensions;

public sealed class TicketingExceptionHandler(
    ILogger<TicketingExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (status, title) = GetProblem(exception);
        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled request failure for {RequestPath}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                "Request {RequestPath} failed with {StatusCode}: {ErrorMessage}",
                httpContext.Request.Path,
                status,
                exception.Message);
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        var detail = environment.IsDevelopment()
            ? exception.Message
            : status >= StatusCodes.Status500InternalServerError
                ? "The request could not be completed."
                : title;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title) GetProblem(Exception exception)
    {
        if (exception is EntityNotFoundException)
        {
            return (StatusCodes.Status404NotFound, "Resource not found");
        }

        if (exception is ConflictException)
        {
            return (StatusCodes.Status409Conflict, "Conflict");
        }

        if (exception is ValidationException)
        {
            return (StatusCodes.Status400BadRequest, "Validation failed");
        }

        if (exception is DomainException)
        {
            return (StatusCodes.Status409Conflict, "Conflict");
        }

        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation
                })
            {
                return (StatusCodes.Status409Conflict, "Conflict");
            }
        }

        return (StatusCodes.Status500InternalServerError, "An unexpected error occurred");
    }
}
