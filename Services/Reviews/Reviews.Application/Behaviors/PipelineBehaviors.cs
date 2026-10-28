using System.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(e => e is not null).ToArray();
        if (failures.Length > 0) throw new ValidationException(failures);
        return await next();
    }
}

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {RequestType}: {@Request}", typeof(TRequest).Name, request);
        var response = await next();
        logger.LogInformation("Handled {RequestType}: {@Response}", typeof(TRequest).Name, response);
        return response;
    }
}

public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();
        if (timer.ElapsedMilliseconds > 500)
            logger.LogWarning("Slow request {RequestType} took {ElapsedMs} ms", typeof(TRequest).Name, timer.ElapsedMilliseconds);
        return response;
    }
}

public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try { return await next(); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Request {RequestType} failed", typeof(TRequest).Name);
            throw;
        }
    }
}
