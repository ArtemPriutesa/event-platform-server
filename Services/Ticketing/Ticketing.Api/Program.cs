using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Ticketing.Api.Extensions;
using Ticketing.Api.Persistence;
using Ticketing.Bll.Mapping;
using Ticketing.Bll.Services;
using Ticketing.Dal.Persistence;
using Ticketing.Dal.Repositories;
using Ticketing.Dal.UnitOfWork;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Exceptions;
using Platform.ServiceDefaults;
using Ticketing.Api.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.AddPlatformSerilog("orders-api");
var connectionString = builder.Configuration.GetConnectionString("Ticketing");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Aspire did not provide the 'Ticketing' connection string. Start the service through Platform.AppHost.");
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TicketingExceptionHandler>();
builder.Services.AddDbContext<TicketingDbContext>(options => options
    .UseNpgsql(connectionString)
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));
builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IEagerReservationRepository, EagerReservationRepository>();
builder.Services.AddScoped<IExplicitReservationRepository, ExplicitReservationRepository>();
builder.Services.AddScoped<IReservationTicketTypeRepository, ReservationTicketTypeRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITicketingService, TicketingService>();
builder.Services.AddValidatorsFromAssemblyContaining<Ticketing.Bll.Validation.TicketTypeQueryValidator>();
builder.Services.AddAutoMapper(configuration =>
{
    configuration.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    configuration.AddProfile<TicketingMappingProfile>();
});

var app = builder.Build();
app.Services.GetRequiredService<AutoMapper.IConfigurationProvider>().AssertConfigurationIsValid();
app.UsePlatformRequestContext();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    await context.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
    {
        await TicketingDbInitializer.SeedAsync(context);
    }
}

app.MapControllers();
app.MapGrpcService<TicketingGrpcService>();
app.MapGet("/health", async (TicketingDbContext context, CancellationToken cancellationToken) =>
{
    var connected = await context.Database.CanConnectAsync(cancellationToken);
    return connected
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable");
}).WithName("TicketingDatabaseHealth");
app.MapHealthChecks("/alive");

app.Run();

public partial class Program;
