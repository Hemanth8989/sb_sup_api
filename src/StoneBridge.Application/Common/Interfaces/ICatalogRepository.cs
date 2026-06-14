using StoneBridge.Application.Catalog.DTOs;
using StoneBridge.Application.Common.Models;

namespace StoneBridge.Application.Common.Interfaces;

/// <summary>
/// Data access contract for the fabricator-facing slab catalog.
/// Implementations must only return slabs that are:
///   - status = 'available'
///   - is_active = TRUE
///   - supplier has an ACTIVE connection with the requesting fabricator
/// These three conditions are enforced at the SQL level via the active connection JOIN.
/// PostgreSQL RLS provides an additional enforcement layer via app.tenant_id session context.
/// </summary>
public interface ICatalogRepository
{
    /// <summary>
    /// Full-text and filtered search of the slab catalog for a connected fabricator.
    /// Returns enriched CatalogSlabDto records with supplier name, pricing, and photo URLs.
    /// </summary>
    Task<PagedResult<CatalogSlabDto>> SearchAsync(
        Guid                fabricatorId,
        CatalogSearchParams searchParams,
        CancellationToken   ct = default);
}