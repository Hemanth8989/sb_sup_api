namespace StoneBridge.Application.Common.Exceptions;

/// <summary>
/// Thrown by ValidationBehaviour when FluentValidation rules fail.
/// Contains a field-level error dictionary for the client to display inline.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(
            errors, StringComparer.OrdinalIgnoreCase).AsReadOnly();
    }

    /// <summary>
    /// Field-level errors. Key = property name. Value = array of error messages.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}