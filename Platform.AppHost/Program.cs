var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var eventCatalogDatabase = postgres.AddDatabase("EventCatalog", "event_catalog");
var mongo = builder.AddMongoDB("mongo").WithDataVolume();
var reviewsDatabase = mongo.AddDatabase("Reviews", "event_platform_payments");

builder.AddProject<Projects.EventCatalog_Api>("event-catalog-api")
    .WithReference(eventCatalogDatabase)
    .WaitFor(eventCatalogDatabase);

builder.AddProject<Projects.Reviews_Api>("reviews-api")
    .WithReference(reviewsDatabase)
    .WaitFor(reviewsDatabase);

builder.Build().Run();
