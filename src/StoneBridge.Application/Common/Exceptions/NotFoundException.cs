namespace StoneBridge.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist or is not accessible to the current tenant.
/// Maps to HTTP 404 Not Found via ExceptionHandlingMiddleware.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object key)
        : base($"Resource '{resourceName}' with key '{key}' was not found.") { }

    public NotFoundException(string message)
        : base(message) { }
}