using Reviews.Api;
using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Mongo;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Reviews")
    ?? builder.Configuration[$"{MongoDbOptions.SectionName}:ConnectionString"];
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure MongoDb:ConnectionString or start the service through Platform.AppHost.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplication(builder.Configuration["AutoMapper:LicenseKey"]);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.Services.GetRequiredService<AutoMapper.IConfigurationProvider>().AssertConfigurationIsValid();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.MapGet("/health", async (MongoDbContext context, CancellationToken cancellationToken) =>
{
    await context.Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1),
        cancellationToken: cancellationToken);
    return Results.Ok(new { status = "healthy", database = "connected" });
}).WithName("MongoDatabaseHealth");
app.Run();

public partial class Program;
