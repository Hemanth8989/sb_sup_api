using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Api.Endpoints;

/// <summary>
/// Health check endpoints — no authentication required.
/// /health      — liveness check (is the process alive?)
/// /health/ready — readiness check (can the process serve traffic?)
/// Railway uses /health for its container health check.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health")
            .WithTags("Health")
            .AllowAnonymous();

        // Liveness: returns 200 if the process is running
        group.MapGet("/", () => Results.Ok(new
        {
            Status    = "Healthy",
            Service   = "StoneBridge.Api",
            Version   = "1.0.0",
            Timestamp = DateTimeOffset.UtcNow,
        }))
        .WithName("HealthLiveness")
        .WithSummary("Liveness check — returns 200 if the API process is running.")
        .Produces<object>(StatusCodes.Status200OK);

        // Readiness: verifies the database is reachable
        group.MapGet("/ready", async (
            IDbConnectionFactory connectionFactory,
            CancellationToken    ct) =>
        {
            try
            {
                using var conn = await connectionFactory.CreateConnectionAsync(ct);
                return Results.Ok(new
                {
                    Status    = "Ready",
                    Database  = "Connected",
                    Timestamp = DateTimeOffset.UtcNow,
                });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new
                    {
                        Status    = "Degraded",
                        Database  = "Unreachable",
                        Error     = ex.Message,
                        Timestamp = DateTimeOffset.UtcNow,
                    },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("HealthReadiness")
        .WithSummary("Readiness check — verifies database connectivity.")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}