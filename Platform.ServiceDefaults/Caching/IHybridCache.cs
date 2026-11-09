namespace Platform.ServiceDefaults.Caching;

public interface IHybridCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan? lifetime = null, CancellationToken cancellationToken = default,
        Func<T, bool>? shouldCache = null);

    Task RemoveAsync(string key, string reason, CancellationToken cancellationToken = default);
}
