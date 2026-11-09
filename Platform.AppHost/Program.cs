using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

const string postgresPasswordName = "postgres-password";
var postgresPasswordValue = builder.Configuration[$"Parameters:{postgresPasswordName}"];
if (string.IsNullOrWhiteSpace(postgresPasswordValue))
    throw new InvalidOperationException("Set the Parameters:postgres-password user secret before starting AppHost.");
var postgresPassword = builder.AddParameter(postgresPasswordName, postgresPasswordValue, secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("event-platform-laba5-postgres-data");
var eventCatalogDatabase = postgres.AddDatabase("EventCatalog", "event_catalog");
var ticketingDatabase = postgres.AddDatabase("Ticketing", "ticketing");

const string mongoPasswordName = "mongo-password";
var mongoPasswordValue = builder.Configuration[$"Parameters:{mongoPasswordName}"];
if (string.IsNullOrWhiteSpace(mongoPasswordValue))
    throw new InvalidOperationException("Set the Parameters:mongo-password user secret before starting AppHost.");
var mongoPassword = builder.AddParameter(mongoPasswordName, mongoPasswordValue, secret: true);
var mongoUsername = builder.AddParameter("mongo-username", "admin");
var mongo = builder.AddMongoDB("mongo", userName: mongoUsername, password: mongoPassword)
    .WithDataVolume("event-platform-laba5-mongo-data");
var reviewsDatabase = mongo.AddDatabase("Reviews", "event_platform_payments");

var catalog = builder.AddProject<Projects.EventCatalog_Api>("catalog-api")
    .WithReference(eventCatalogDatabase)
    .WaitFor(eventCatalogDatabase);

var orders = builder.AddProject<Projects.Ticketing_Api>("orders-api")
    .WithHttpEndpoint(port: 5003, name: "http")
    .WithReference(ticketingDatabase)
    .WaitFor(ticketingDatabase);

var reviews = builder.AddProject<Projects.Reviews_Api>("reviews-api")
    .WithHttpEndpoint(port: 5004, name: "http")
    .WithReference(reviewsDatabase)
    .WaitFor(reviewsDatabase);

var aggregator = builder.AddProject<Projects.Aggregator>("aggregator-api")
    .WithHttpEndpoint(port: 5005, name: "http")
    .WithReference(orders)
    .WithReference(reviews)
    .WaitFor(orders)
    .WaitFor(reviews);

builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithReference(catalog)
    .WithReference(orders)
    .WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(catalog)
    .WaitFor(orders)
    .WaitFor(reviews)
    .WaitFor(aggregator);

builder.Build().Run();
