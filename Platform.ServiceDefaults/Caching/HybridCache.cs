using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults.Caching;

public sealed class HybridCache(
    IMemoryCache memory,
    IDistributedCache distributed,
    CacheTelemetry telemetry,
    ILogger<HybridCache> logger) : IHybridCache
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LocalLifetime = TimeSpan.FromSeconds(30);

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan? lifetime = null, CancellationToken cancellationToken = default,
        Func<T, bool>? shouldCache = null)
    {
        if (memory.TryGetValue(key, out T? local) && local is not null)
        {
            telemetry.Hit("L1");
            logger.LogInformation("Cache hit at {CacheLevel} for {CacheKey}", "L1", key);
            return local;
        }

        try
        {
            var bytes = await distributed.GetAsync(key, cancellationToken);
            if (bytes is { Length: > 0 })
            {
                var cached = JsonSerializer.Deserialize<T>(bytes);
                if (cached is not null)
                {
                    telemetry.Hit("L2");
                    logger.LogInformation("Cache hit at {CacheLevel} for {CacheKey}", "L2", key);
                    memory.Set(key, cached, CreateLocalOptions());
                    return cached;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache read failed for {CacheKey}; loading from the source", key);
        }

        telemetry.Miss();
        logger.LogInformation("Cache miss for {CacheKey}; loading from the source", key);
        var value = await factory(cancellationToken);
        if (shouldCache is not null && !shouldCache(value))
        {
            logger.LogInformation("Skipping cache write for {CacheKey} because the response is incomplete", key);
            return value;
        }

        var ttl = lifetime ?? DefaultLifetime;
        memory.Set(key, value, CreateLocalOptions());
        try
        {
            await distributed.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache write failed for {CacheKey}", key);
        }

        return value;
    }

    private static MemoryCacheEntryOptions CreateLocalOptions()
        => new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetSlidingExpiration(TimeSpan.FromSeconds(15))
            .SetAbsoluteExpiration(LocalLifetime);

    public async Task RemoveAsync(string key, string reason, CancellationToken cancellationToken = default)
    {
        memory.Remove(key);
        try
        {
            await distributed.RemoveAsync(key, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache invalidation failed for {CacheKey}; reason: {Reason}", key, reason);
        }

        logger.LogInformation("Cache invalidated for {CacheKey}; reason: {Reason}", key, reason);
    }
}
