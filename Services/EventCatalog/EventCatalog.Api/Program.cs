using EventCatalog.Bll.Events;
using EventCatalog.Dal.Events;
using EventCatalog.Dal.Connection;
using EventCatalog.Dal.Repositories;
using EventCatalog.Dal.UnitOfWork;
using EventCatalog.Api.Extensions;
using EventCatalog.Domain.Catalog;
using Npgsql;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("EventCatalog");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing connection string 'EventCatalog'. Set the ConnectionStrings__EventCatalog environment variable.");
}

DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<IDbSession, DbSession>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ICategoryRepository, AdoNetCategoryRepository>();
builder.Services.AddScoped<IOrganizerRepository, AdoNetOrganizerRepository>();
builder.Services.AddScoped<ITagRepository, AdoNetTagRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddAutoMapper(configuration =>
    configuration.AddMaps(typeof(EventCatalogMappingProfile).Assembly));

var app = builder.Build();

app.Services.GetRequiredService<AutoMapper.IConfigurationProvider>().AssertConfigurationIsValid();
app.UseMiddleware<EventCatalogExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapGet("/health", async (NpgsqlDataSource dataSource, CancellationToken cancellationToken) =>
{
    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
    await using var command = new NpgsqlCommand("SELECT 1;", connection);
    await command.ExecuteScalarAsync(cancellationToken);
    return Results.Ok(new { status = "healthy", database = "connected" });
}).WithName("DatabaseHealth");

app.Run();
