using System.Text.Json;
using EventCatalog.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventCatalog.Api.Extensions;

public sealed class EventCatalogExceptionMiddleware(
    RequestDelegate next,
    ILogger<EventCatalogExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions ProblemJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Path} was cancelled by the client.", context.Request.Path);
        }
        catch (Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                EventNotFoundException or CatalogReferenceNotFoundException
                    => (StatusCodes.Status404NotFound, "Resource not found"),
                EventValidationException
                    => (StatusCodes.Status400BadRequest, "Validation failed"),
                EventConflictException
                    => (StatusCodes.Status409Conflict, "Conflict"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled error while processing {Path}.", context.Request.Path);
            }
            else
            {
                logger.LogInformation(exception, "Request to {Path} failed with status {StatusCode}.", context.Request.Path, statusCode);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(context.Response.Body, new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "The request could not be completed."
                    : exception.Message,
                Instance = context.Request.Path
            }, ProblemJsonOptions, context.RequestAborted);
        }
    }
}
