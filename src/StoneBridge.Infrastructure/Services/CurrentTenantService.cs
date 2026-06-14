using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Infrastructure.Services;

/// <summary>
/// Reads the current authenticated tenant context from validated JWT claims
/// in the HTTP request via IHttpContextAccessor.
/// Registered as Scoped — one instance per HTTP request lifetime.
/// Throws UnauthorizedAccessException when required claims are absent (maps to 401 via middleware).
/// Never trust request body values for tenant identity — always read from validated JWT.
/// </summary>
public sealed class CurrentTenantService : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException(
            "No HTTP context available. " +
            "ICurrentTenant cannot be used outside of an HTTP request context.");

    /// <inheritdoc />
    public Guid TenantId
    {
        get
        {
            var claim = User.FindFirstValue("tenant_id")
                ?? throw new UnauthorizedAccessException(
                    "The 'tenant_id' claim is missing from the JWT token. " +
                    "Ensure Clerk is configured to include this custom claim.");

            return Guid.TryParse(claim, out var id)
                ? id
                : throw new UnauthorizedAccessException(
                    $"The 'tenant_id' claim value '{claim}' is not a valid GUID.");
        }
    }

    /// <inheritdoc />
    public string TenantType =>
        User.FindFirstValue("tenant_type")
        ?? throw new UnauthorizedAccessException(
            "The 'tenant_type' claim is missing from the JWT token.");

    /// <inheritdoc />
    public Guid UserId
    {
        get
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub")
                      ?? throw new UnauthorizedAccessException(
                          "No user identifier claim found in the JWT token.");

            return Guid.TryParse(claim, out var id)
                ? id
                : throw new UnauthorizedAccessException(
                    $"The user identifier claim '{claim}' is not a valid GUID.");
        }
    }

    /// <inheritdoc />
    public string Role =>
        User.FindFirstValue("role")
        ?? User.FindFirstValue(ClaimTypes.Role)
        ?? "viewer";
}