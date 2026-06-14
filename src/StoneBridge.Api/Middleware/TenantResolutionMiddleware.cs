namespace StoneBridge.Api.Middleware;

/// <summary>
/// Runs AFTER UseAuthentication() so JWT claims are already validated.
/// Extracts tenant_id, tenant_type, user_id from claims and stores them in HttpContext.Items
/// for fast access without re-parsing the JWT on every HttpContext access.
/// ICurrentTenant reads directly from HttpContext.User claims — this middleware
/// is a complementary caching layer for non-DI code paths.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate                   _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate                    next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId   = context.User.FindFirst("tenant_id")?.Value;
            var tenantType = context.User.FindFirst("tenant_type")?.Value;
            var userId     = context.User.FindFirst("sub")?.Value
                           ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (tenantId is not null)
            {
                context.Items["TenantId"]   = tenantId;
                context.Items["TenantType"] = tenantType;
                context.Items["UserId"]     = userId;
            }
            else
            {
                _logger.LogWarning(
                    "Authenticated request missing 'tenant_id' claim | Path: {Path}",
                    context.Request.Path);
            }
        }

        await _next(context);
    }
}