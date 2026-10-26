var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var eventCatalogDatabase = postgres.AddDatabase("EventCatalog", "event_catalog");
var ticketingDatabase = postgres.AddDatabase("Ticketing", "ticketing");

builder.AddProject<Projects.EventCatalog_Api>("event-catalog-api")
    .WithReference(eventCatalogDatabase)
    .WaitFor(eventCatalogDatabase);

builder.AddProject<Projects.Ticketing_Api>("ticketing-api")
    .WithReference(ticketingDatabase)
    .WaitFor(ticketingDatabase);

builder.Build().Run();
