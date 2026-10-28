using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Domain.Exceptions;

namespace Reviews.Api;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Request validation failed"),
            DomainException => (StatusCodes.Status400BadRequest, "Domain rule violated"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Resource conflict"),
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
                => (StatusCodes.Status409Conflict, "Duplicate resource"),
            MongoConnectionException => (StatusCodes.Status503ServiceUnavailable, "Database unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
        };

        logger.Log(status >= 500 ? LogLevel.Error : LogLevel.Information, exception,
            "Request failed with HTTP {StatusCode}", status);
        context.Response.StatusCode = status;
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage).ToArray());
            await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(errors)
            {
                Status = status,
                Title = title,
                Instance = context.Request.Path
            }, cancellationToken);
        }
        else
        {
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status < 500 ? exception.Message : null,
                Instance = context.Request.Path
            }, cancellationToken);
        }
        return true;
    }
}
