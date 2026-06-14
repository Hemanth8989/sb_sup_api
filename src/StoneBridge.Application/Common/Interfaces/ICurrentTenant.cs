namespace StoneBridge.Application.Common.Interfaces;

/// <summary>
/// Provides the authenticated tenant context for the current HTTP request.
/// Implemented by CurrentTenantService (Infrastructure layer).
/// Injected into every command and query handler that needs to know who is calling.
/// All values are sourced from validated JWT claims — never trust request body values for tenant identity.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>The tenant's UUID. From the 'tenant_id' JWT claim.</summary>
    Guid TenantId { get; }

    /// <summary>'supplier' or 'fabricator'. From the 'tenant_type' JWT claim.</summary>
    string TenantType { get; }

    /// <summary>The authenticated user's UUID. From the JWT 'sub' claim.</summary>
    Guid UserId { get; }

    /// <summary>The user's role within their tenant. From the 'role' JWT claim.</summary>
    string Role { get; }

    /// <summary>True when TenantType == 'supplier' (case-insensitive).</summary>
    bool IsSupplier => string.Equals(TenantType, "supplier", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when TenantType == 'fabricator' (case-insensitive).</summary>
    bool IsFabricator => string.Equals(TenantType, "fabricator", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when Role is 'owner' or 'admin'.</summary>
    bool IsAdminOrOwner => Role is "owner" or "admin";
}