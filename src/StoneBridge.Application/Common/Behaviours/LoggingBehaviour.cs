using MediatR;
using Microsoft.Extensions.Logging;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour: logs every request entering and leaving the pipeline.
/// Logs at Information level on entry and exit.
/// Logs at Error level on exception (and re-throws — does not swallow).
/// Runs AFTER ValidationBehaviour so invalid requests are not logged as full requests.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
    ICurrentTenant                                 currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 cancellationToken)
    {
        var name = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling {RequestName} | TenantId: {TenantId} | UserId: {UserId}",
            name,
            currentTenant.TenantId,
            currentTenant.UserId);

        try
        {
            var response = await next();

            logger.LogInformation(
                "Handled {RequestName} successfully | TenantId: {TenantId}",
                name,
                currentTenant.TenantId);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Request {RequestName} failed | TenantId: {TenantId} | Error: {Error}",
                name,
                currentTenant.TenantId,
                ex.Message);

            throw;
        }
    }
}