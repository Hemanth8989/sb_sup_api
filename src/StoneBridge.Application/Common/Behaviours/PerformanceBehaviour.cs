using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour: measures handler execution time.
/// Logs Warning when elapsed > 500ms — indicates a query needing an index.
/// Logs Error when elapsed > 1000ms — indicates a critical performance problem.
/// Does NOT affect the request outcome — purely observational.
/// </summary>
public sealed class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<PerformanceBehaviour<TRequest, TResponse>> logger,
    ICurrentTenant                                     currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;
    private const int ErrorThresholdMs   = 1_000;

    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        var ms = sw.ElapsedMilliseconds;

        if (ms >= ErrorThresholdMs)
        {
            logger.LogError(
                "SLOW REQUEST > {Threshold}ms | {RequestName} took {Elapsed}ms | TenantId: {TenantId}",
                ErrorThresholdMs,
                typeof(TRequest).Name,
                ms,
                currentTenant.TenantId);
        }
        else if (ms >= WarningThresholdMs)
        {
            logger.LogWarning(
                "Slow request > {Threshold}ms | {RequestName} took {Elapsed}ms | TenantId: {TenantId}",
                WarningThresholdMs,
                typeof(TRequest).Name,
                ms,
                currentTenant.TenantId);
        }

        return response;
    }
}