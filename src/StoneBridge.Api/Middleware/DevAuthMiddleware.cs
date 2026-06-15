using System.Security.Claims;

namespace StoneBridge.Api.Middleware;

/// <summary>
/// Development-only middleware that injects a fake authenticated tenant
/// without requiring a real Clerk JWT. Enabled via DevAuth:Enabled = true
/// in appsettings.Development.json. NEVER active in production.
/// </summary>
public sealed class DevAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DevAuthOptions  _options;

    public DevAuthMiddleware(RequestDelegate next, DevAuthOptions options)
    {
        _next   = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Inject fake identity — replaces whatever JWT middleware parsed
        var claims = new[]
        {
            new Claim("tenant_id",   _options.TenantId),
            new Claim("tenant_type", _options.TenantType),
            new Claim("role",        _options.Role),
            new Claim(ClaimTypes.NameIdentifier, _options.UserId),
            new Claim("sub",         _options.UserId),
        };

        var identity  = new ClaimsIdentity(claims, authenticationType: "DevAuth");
        var principal = new ClaimsPrincipal(identity);
        context.User  = principal;

        await _next(context);
    }
}

public sealed class DevAuthOptions
{
    public bool   Enabled    { get; set; }
    public string TenantId   { get; set; } = string.Empty;
    public string TenantType { get; set; } = "supplier";
    public string UserId     { get; set; } = string.Empty;
    public string Role       { get; set; } = "owner";
}
