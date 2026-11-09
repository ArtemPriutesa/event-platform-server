using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults;

public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var started = Stopwatch.GetTimestamp();
        var status = StatusCode.OK;
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException exception)
        {
            status = exception.StatusCode;
            logger.LogWarning(exception,
                "gRPC call {Method} from {Peer} failed with {StatusCode} after {ElapsedMs} ms",
                context.Method, context.Peer, status, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            status = StatusCode.Internal;
            logger.LogError(exception,
                "gRPC call {Method} from {Peer} failed with {StatusCode} after {ElapsedMs} ms",
                context.Method, context.Peer, status, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
        finally
        {
            logger.LogInformation(
                "gRPC call {Method} from {Peer} completed with {StatusCode} in {ElapsedMs} ms",
                context.Method, context.Peer, status, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
