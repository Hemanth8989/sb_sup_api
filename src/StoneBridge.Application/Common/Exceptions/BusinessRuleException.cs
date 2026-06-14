namespace StoneBridge.Application.Common.Exceptions;

/// <summary>
/// Thrown when a domain business rule is violated.
/// Maps to HTTP 400 Bad Request.
/// Examples: deleting a reserved slab, cancelling a closed PO.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message)
        : base(message)
    {
        RuleName = ruleName;
    }
}