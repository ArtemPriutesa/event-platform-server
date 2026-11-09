using System.Diagnostics.Metrics;

namespace Platform.ServiceDefaults.Caching;

public sealed class CacheTelemetry : IDisposable
{
    public const string MeterName = "EventPlatform.Caching";
    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;

    public CacheTelemetry()
    {
        _hits = _meter.CreateCounter<long>("cache.hits", "items");
        _misses = _meter.CreateCounter<long>("cache.misses", "items");
    }

    public void Hit(string level) => _hits.Add(1, new KeyValuePair<string, object?>("level", level));
    public void Miss() => _misses.Add(1);
    public void Dispose() => _meter.Dispose();
}
