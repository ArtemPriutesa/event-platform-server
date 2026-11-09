using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Core;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Caching;
using Platform.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.AddPlatformSerilog("aggregator-api");
builder.Services.AddHttpClient<OrdersApiClient>(client => { client.BaseAddress = new Uri("https+http://orders-api"); client.Timeout = TimeSpan.FromSeconds(4); });
builder.Services.AddHttpClient<CatalogApiClient>(client => { client.BaseAddress = new Uri("https+http://catalog-api"); client.Timeout = TimeSpan.FromSeconds(4); });
builder.Services.AddHttpClient<ReviewsApiClient>(client => { client.BaseAddress = new Uri("https+http://reviews-api"); client.Timeout = TimeSpan.FromSeconds(4); });
builder.Services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(client => client.Address = new Uri("https://catalog-api"));
builder.Services.AddGrpcClient<TicketingGrpc.TicketingGrpcClient>(client => client.Address = new Uri("https://orders-api"));
builder.Services.AddGrpcClient<ReviewsGrpc.ReviewsGrpcClient>(client => client.Address = new Uri("https://reviews-api"));
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
app.MapGet("/api/aggregator/grpc/{reservationId:long}/{eventId:long}/{eventPublicId:guid}", async (
    long reservationId,
    long eventId,
    Guid eventPublicId,
    TicketingGrpc.TicketingGrpcClient ticketing,
    CatalogGrpc.CatalogGrpcClient catalog,
    ReviewsGrpc.ReviewsGrpcClient reviews,
    IHybridCache cache,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var key = $"aggregator:grpc:{reservationId}:{eventId}:{eventPublicId:D}";
    var response = await cache.GetOrCreateAsync(key, async token =>
    {
        var reservationTask = CaptureGrpcAsync("ticketing", () => ticketing.GetReservationAsync(
            new GetReservationRequest { Id = reservationId }, deadline: DateTime.UtcNow.AddSeconds(4), cancellationToken: token).ResponseAsync, logger, token);
        var eventTask = CaptureGrpcAsync("catalog", () => catalog.GetEventAsync(
            new GetEventRequest { Id = eventId }, deadline: DateTime.UtcNow.AddSeconds(4), cancellationToken: token).ResponseAsync, logger, token);
        var reviewsTask = CaptureGrpcAsync("reviews", () => reviews.GetReviewsForEventAsync(
            new GetReviewsRequest { EventPublicId = eventPublicId.ToString() }, deadline: DateTime.UtcNow.AddSeconds(4), cancellationToken: token).ResponseAsync, logger, token);
        await Task.WhenAll(reservationTask, eventTask, reviewsTask);
        var reservation = await reservationTask;
        var eventReply = await eventTask;
        var reviewReply = await reviewsTask;
        var reservationSummary = reservation is null ? null : new ReservationSummary(reservation.Id,
            reservation.PublicId, reservation.UserPublicId, reservation.Status,
            reservation.Items.Select(item => new ReservationLineSummary(item.EventPublicId, item.EventTitle,
                item.TicketType, item.Quantity)).ToArray());
        var eventSummary = eventReply is null ? null : new EventSummary(eventReply.Id, eventReply.PublicId,
            eventReply.Title, eventReply.Status, eventReply.StartsAt, eventReply.EndsAt);
        var reviewSummaries = reviewReply?.Reviews.Select(review => new ReviewSummary(review.Id,
            review.EventPublicId, review.Author, review.Rating, review.Text)).ToArray();
        return new AggregatedGrpcResponse(reservationSummary, eventSummary, reviewSummaries,
            reservation is not null && eventReply is not null && reviewReply is not null);
    }, TimeSpan.FromSeconds(45), cancellationToken,
        shouldCache: result => result.IsComplete);
    return Results.Ok(response);
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

static async Task<T?> CaptureGrpcAsync<T>(string service, Func<Task<T>> request, ILogger logger,
    CancellationToken cancellationToken)
{
    try { return await request(); }
    catch (RpcException exception)
    {
        if (exception.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
            throw;
        logger.LogWarning(exception, "gRPC call to {ServiceName} failed with {StatusCode}", service, exception.StatusCode);
        return default;
    }
    catch (HttpRequestException exception)
    {
        logger.LogWarning(exception, "gRPC call to {ServiceName} could not connect", service);
        return default;
    }
    catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
    {
        logger.LogWarning(exception, "gRPC call to {ServiceName} timed out", service);
        return default;
    }
}

public sealed record AggregatedOrderResponse(JsonElement? Order, IReadOnlyList<JsonElement> Events,
    IReadOnlyList<JsonElement> Reviews, IReadOnlyList<string> Warnings);
public sealed record AggregatedGrpcResponse(ReservationSummary? Reservation, EventSummary? Event,
    IReadOnlyList<ReviewSummary>? Reviews, bool IsComplete);
public sealed record ReservationSummary(long Id, string PublicId, string UserPublicId, string Status,
    IReadOnlyList<ReservationLineSummary> Items);
public sealed record ReservationLineSummary(string EventPublicId, string EventTitle, string TicketType, int Quantity);
public sealed record EventSummary(long Id, string PublicId, string Title, string Status, string StartsAt, string EndsAt);
public sealed record ReviewSummary(string Id, string EventPublicId, string Author, int Rating, string Text);
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
