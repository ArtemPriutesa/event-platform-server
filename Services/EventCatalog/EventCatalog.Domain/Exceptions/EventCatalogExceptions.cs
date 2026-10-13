namespace EventCatalog.Domain.Exceptions;

public abstract class EventCatalogException(string message) : Exception(message);

public sealed class EventNotFoundException(long id)
    : EventCatalogException($"Event {id} was not found.");

public sealed class CatalogReferenceNotFoundException(string reference)
    : EventCatalogException($"{reference} was not found.");

public sealed class EventConflictException(string message)
    : EventCatalogException(message);

public sealed class EventValidationException(string message)
    : EventCatalogException(message);
