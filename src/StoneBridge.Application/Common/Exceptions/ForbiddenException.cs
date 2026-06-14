namespace StoneBridge.Application.Common.Exceptions;

/// <summary>
/// Thrown when the authenticated tenant attempts an operation they are not permitted to perform.
/// Maps to HTTP 403 Forbidden.
/// Example: a supplier tenant calling a fabricator-only endpoint.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException()
        : base("You do not have permission to perform this operation.") { }
}