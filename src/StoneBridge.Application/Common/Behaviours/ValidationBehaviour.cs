using FluentValidation;
using MediatR;
using ValidationException = StoneBridge.Application.Common.Exceptions.ValidationException;

namespace StoneBridge.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour: runs all FluentValidation validators registered
/// for TRequest before the handler executes.
/// If any validator fails → throws ValidationException → handler never runs.
/// If no validators are registered → passes through immediately.
/// Registered as the FIRST behaviour so invalid requests are rejected cheapest.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(
                f => f.PropertyName,
                f => f.ErrorMessage,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}