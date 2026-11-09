using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.AddPlatformSerilog("aggregator-api");
builder.Services.AddHttpClient<OrdersApiClient>(client => { client.BaseAddress = new Uri("https+http://orders-api"); client.Timeout = TimeSpan.FromSeconds(4); });
builder.Services.AddHttpClient<CatalogApiClient>(client => { client.BaseAddress = new Uri("https+http://catalog-api"); client.Timeout = TimeSpan.FromSeconds(4); });
builder.Services.AddHttpClient<ReviewsApiClient>(client => { client.BaseAddress = new Uri("https+http://reviews-api"); client.Timeout = TimeSpan.FromSeconds(4); });
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();
app.UsePlatformRequestContext();
app.MapGet("/api/aggregator/orders/{id:long}/full", async (
    long id,
    OrdersApiClient orders,
    CatalogApiClient catalog,
    ReviewsApiClient reviews,
    ActivitySource activities,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    using var activity = activities.StartActivity("aggregate-order", ActivityKind.Internal);
    activity?.SetTag("order.id", id);
    var warnings = new List<string>();
    JsonElement? order = null;
    IReadOnlyList<JsonElement> events = [];
    IReadOnlyList<JsonElement> reviewItems = [];

    try
    {
        var started = Stopwatch.GetTimestamp();
        order = await orders.GetReservationAsync(id, cancellationToken);
        logger.LogInformation("Aggregator call to {ServiceName} for order {OrderId} completed in {ElapsedMs} ms", "orders-api", id, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
    catch (Exception exception) when (exception is HttpRequestException || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
    {
        warnings.Add("Orders service is temporarily unavailable.");
        logger.LogWarning(exception, "Orders request failed for {OrderId}", id);
    }

    if (order is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
    {
        var eventSnapshots = items.EnumerateArray()
            .Where(item => item.TryGetProperty("eventPublicId", out _) && item.TryGetProperty("eventTitleSnapshot", out _))
            .Select(item => new EventSnapshot(item.GetProperty("eventPublicId").GetString()!, item.GetProperty("eventTitleSnapshot").GetString()!))
            .DistinctBy(item => item.PublicId)
            .ToArray();

        var results = await Task.WhenAll(eventSnapshots.Select(async snapshot =>
        {
            var catalogTask = CaptureAsync("catalog", () => catalog.SearchEventsAsync(snapshot.PublicId, snapshot.Title, cancellationToken), warnings, logger, cancellationToken);
            var reviewsTask = CaptureAsync("reviews", () => reviews.GetForEventAsync(snapshot.PublicId, cancellationToken), warnings, logger, cancellationToken);
            var results = await Task.WhenAll(catalogTask, reviewsTask);
            return (catalogResult: results[0], reviewResult: results[1]);
        }));

        events = results.SelectMany(result => result.catalogResult).ToArray();
        reviewItems = results.SelectMany(result => result.reviewResult).ToArray();
    }

    return Results.Ok(new AggregatedOrderResponse(order, events, reviewItems, warnings));
});
app.MapHealthChecks("/alive");
app.Run();

static async Task<IReadOnlyList<JsonElement>> CaptureAsync(
    string service,
    Func<Task<IReadOnlyList<JsonElement>>> request,
    ICollection<string> warnings,
    ILogger logger,
    CancellationToken cancellationToken)
{
    var started = Stopwatch.GetTimestamp();
    try
    {
        var result = await request();
        logger.LogInformation("Aggregator call to {ServiceName} completed in {ElapsedMs} ms", service, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return result;
    }
    catch (Exception exception) when (exception is HttpRequestException || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
    {
        lock (warnings) warnings.Add($"{service} service is temporarily unavailable.");
        logger.LogWarning(exception, "{ServiceName} request failed while aggregating an order", service);
        return [];
    }
}

public sealed record AggregatedOrderResponse(JsonElement? Order, IReadOnlyList<JsonElement> Events,
    IReadOnlyList<JsonElement> Reviews, IReadOnlyList<string> Warnings);
public sealed record EventSnapshot(string PublicId, string Title);

public sealed class OrdersApiClient(HttpClient client)
{
    public async Task<JsonElement?> GetReservationAsync(long id, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"/api/reservations/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }
}

public sealed class CatalogApiClient(HttpClient client)
{
    public async Task<IReadOnlyList<JsonElement>> SearchEventsAsync(string publicId, string title, CancellationToken cancellationToken)
    {
        var path = $"/api/events?search={Uri.EscapeDataString(title)}";
        using var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<JsonElement>>(cancellationToken: cancellationToken) ?? [];
        return results.Where(item => item.TryGetProperty("publicId", out var id)
            && string.Equals(id.GetString(), publicId, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

public sealed class ReviewsApiClient(HttpClient client)
{
    public async Task<IReadOnlyList<JsonElement>> GetForEventAsync(string eventPublicId, CancellationToken cancellationToken)
    {
        var path = $"/api/reviews?eventPublicId={Uri.EscapeDataString(eventPublicId)}";
        using var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<JsonElement>>(cancellationToken: cancellationToken) ?? [];
    }
}
