using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdHandler(IHttpContextAccessor contextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = contextAccessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationId))
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        return base.SendAsync(request, cancellationToken);
    }
}
