var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var eventCatalogDatabase = postgres.AddDatabase("EventCatalog", "event_catalog");

builder.AddProject<Projects.EventCatalog_Api>("event-catalog-api")
    .WithReference(eventCatalogDatabase)
    .WaitFor(eventCatalogDatabase);

builder.Build().Run();
