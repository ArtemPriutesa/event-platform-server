using Microsoft.Extensions.ServiceDiscovery.Yarp;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.AddPlatformSerilog("api-gateway");
builder.Services.AddReverseProxy()
    .AddServiceDiscoveryDestinationResolver()
    .LoadFromMemory(
    [
        new RouteConfig { RouteId = "catalog", ClusterId = "catalog-api", Match = new RouteMatch { Path = "/api/catalog/{**catch-all}" }, Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = "/api/catalog" }, new Dictionary<string, string> { ["PathPrefix"] = "/api" }] },
        new RouteConfig { RouteId = "orders", ClusterId = "orders-api", Match = new RouteMatch { Path = "/api/orders/{**catch-all}" }, Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = "/api/orders" }, new Dictionary<string, string> { ["PathPrefix"] = "/api" }] },
        new RouteConfig { RouteId = "reviews", ClusterId = "reviews-api", Match = new RouteMatch { Path = "/api/reviews/{**catch-all}" } },
        new RouteConfig { RouteId = "aggregator", ClusterId = "aggregator-api", Order = -10, Match = new RouteMatch { Path = "/api/aggregator/{**catch-all}" } }
    ],
    [
        new ClusterConfig { ClusterId = "catalog-api", Destinations = new Dictionary<string, DestinationConfig> { ["catalog"] = new() { Address = "https+http://catalog-api" } } },
        new ClusterConfig { ClusterId = "orders-api", Destinations = new Dictionary<string, DestinationConfig> { ["orders"] = new() { Address = "https+http://orders-api" } } },
        new ClusterConfig { ClusterId = "reviews-api", Destinations = new Dictionary<string, DestinationConfig> { ["reviews"] = new() { Address = "https+http://reviews-api" } } },
        new ClusterConfig { ClusterId = "aggregator-api", Destinations = new Dictionary<string, DestinationConfig> { ["aggregator"] = new() { Address = "https+http://aggregator-api" } } }
    ]);

var app = builder.Build();
app.UsePlatformRequestContext();
app.Use(async (context, next) =>
{
    var started = System.Diagnostics.Stopwatch.GetTimestamp();
    try { await next(); }
    finally
    {
        var route = context.GetEndpoint()?.DisplayName ?? "unmatched";
        if (context.Response.StatusCode is StatusCodes.Status502BadGateway or StatusCodes.Status503ServiceUnavailable)
            app.Logger.LogWarning("Gateway destination unavailable: {Method} {Path} via {Route} -> {StatusCode} in {ElapsedMs} ms from {RemoteIp}", context.Request.Method, context.Request.Path, route, context.Response.StatusCode, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.Connection.RemoteIpAddress?.ToString());
        else
            app.Logger.LogInformation("Gateway {Method} {Path} via {Route} -> {StatusCode} in {ElapsedMs} ms from {RemoteIp}", context.Request.Method, context.Request.Path, route, context.Response.StatusCode, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.Connection.RemoteIpAddress?.ToString());
    }
});
app.MapReverseProxy();
app.MapHealthChecks("/alive");
app.Run();
