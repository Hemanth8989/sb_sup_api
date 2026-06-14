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

        // ── Swagger UI ────────────────────────────────────────────────────
        // JSON spec: /swagger/v1/swagger.json
        // Swagger UI: /swagger
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "StoneBridge API v1");
            c.RoutePrefix        = "swagger";
            c.DocumentTitle      = "StoneBridge API";
            c.DisplayRequestDuration();
            c.DefaultModelsExpandDepth(-1);   // collapse schema models by default
        });

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