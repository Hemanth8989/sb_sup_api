using Scalar.AspNetCore;
using StoneBridge.Api.Middleware;

namespace StoneBridge.Api.Extensions;

/// <summary>
/// Configures the ASP.NET Core middleware pipeline and endpoint routing.
/// Called from Program.cs: app.ConfigurePipeline().
/// Middleware order is critical — document every position change.
/// </summary>
public static class ApplicationBuilderExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // ── Global exception handling ─────────────────────────────────────
        // MUST be first — catches exceptions from ALL downstream middleware.
        // Converts domain exceptions to RFC 7807 ProblemDetails responses.
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // ── Request logging ───────────────────────────────────────────────
        // AFTER exception handler so failed requests are still logged with status codes.
        // Assigns correlation IDs and logs method, path, status, elapsed ms.
        app.UseMiddleware<RequestLoggingMiddleware>();

        // ── Development tooling ───────────────────────────────────────────
        if (app.Environment.IsDevelopment())
        {
            // Scalar API reference at /scalar/v1 (prettier than Swagger UI)
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.Title = "StoneBridge API";
                options.Theme = ScalarTheme.Moon;
            });
        }

        // ── Security headers ──────────────────────────────────────────────
        app.UseHttpsRedirection();
        app.UseCors("StoneBridgeCors");

        // ── Authentication and authorisation ──────────────────────────────
        app.UseAuthentication();
        app.UseAuthorization();

        // ── Tenant resolution ─────────────────────────────────────────────
        // AFTER authentication so JWT claims are already validated and available.
        // Extracts tenant_id, tenant_type, user_id from claims into HttpContext.Items.
        app.UseMiddleware<TenantResolutionMiddleware>();

        // ── Feature endpoints ─────────────────────────────────────────────
        app.MapStoneBridgeEndpoints();

        return app;
    }
}