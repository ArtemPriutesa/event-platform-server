namespace Ticketing.Domain.Exceptions;

public class DomainException(string message) : Exception(message);

public sealed class ConflictException(string message) : DomainException(message);

public sealed class EntityNotFoundException(string entity, object id)
    : Exception($"{entity} '{id}' was not found.");

public sealed class ValidationException(string message) : DomainException(message);
