using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Platform.ServiceDefaults;

public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.Services.AddSingleton(new System.Diagnostics.ActivitySource(builder.Environment.ApplicationName));
        builder.Services.AddServiceDiscovery();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdHandler>();
        builder.Services.ConfigureHttpClientDefaults(http => http
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddServiceDiscovery());
        return builder;
    }

    public static WebApplication UsePlatformRequestContext(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.Use(async (context, next) =>
        {
            var source = context.RequestServices.GetRequiredService<System.Diagnostics.ActivitySource>();
            using var activity = source.StartActivity("business.request", System.Diagnostics.ActivityKind.Internal);
            activity?.SetTag("http.request.method", context.Request.Method);
            activity?.SetTag("http.route", context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value);
            try
            {
                await next();
                activity?.SetTag("http.response.status_code", context.Response.StatusCode);
                if (context.Response.StatusCode >= 500) activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
            }
            catch (Exception exception)
            {
                activity?.AddException(exception);
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, exception.Message);
                throw;
            }
        });
        return app;
    }

    public static void AddPlatformSerilog(this ConfigureHostBuilder host, string serviceName)
    {
        host.UseSerilog((context, configuration) => configuration
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ServiceName", serviceName)
            .MinimumLevel.Is(context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.Console(new CompactJsonFormatter())
            .WriteTo.OpenTelemetry(options =>
            {
                var endpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (!string.IsNullOrWhiteSpace(endpoint)) options.Endpoint = endpoint;
                options.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = serviceName };
            }));
    }

    private static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                .AddHttpClientInstrumentation(options => options.RecordException = true)
                .AddSqlClientInstrumentation(options => options.RecordException = true)
                .AddSource("Npgsql", "MongoDB.Driver.Core.Extensions.DiagnosticSources")
                .AddOtlpExporter());
        return builder;
    }
}
