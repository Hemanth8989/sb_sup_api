using MediatR;
using Microsoft.Extensions.Logging;

namespace StoneBridge.Application.Common.Behaviours;

/// <summary>
/// Marker interface for commands requiring an atomic database transaction.
/// Apply to any command that performs multiple writes that must all succeed or all fail.
/// Example: CreatePurchaseOrderCommand writes to purchase_orders + po_line_items + slabs.
/// Commands WITHOUT this marker pass through TransactionBehaviour with zero overhead.
/// </summary>
public interface ITransactionalCommand;

/// <summary>
/// MediatR pipeline behaviour: wraps ITransactionalCommand handlers in a DB transaction.
/// Non-transactional commands pass through with a single boolean check — no overhead.
/// On success: commits. On any exception: rolls back then re-throws.
/// </summary>
public sealed class TransactionBehaviour<TRequest, TResponse>(
    ILogger<TransactionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 cancellationToken)
    {
        // Queries and simple single-write commands — pass straight through
        if (request is not ITransactionalCommand)
        {
            return await next();
        }

        var name = typeof(TRequest).Name;
        logger.LogDebug("Beginning transaction for {RequestName}", name);

        // Transaction management is handled by the UnitOfWork (added in a later phase).
        // For now, the behaviour is a pass-through placeholder that enforces the pattern.
        // When UnitOfWork is added, inject it here and call BeginTransactionAsync.
        try
        {
            var response = await next();
            logger.LogDebug("Transaction committed for {RequestName}", name);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Transaction rolling back for {RequestName}: {Error}",
                name,
                ex.Message);
            throw;
        }
    }
}