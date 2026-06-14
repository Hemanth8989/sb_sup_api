using MediatR;
using StoneBridge.Application.Catalog.DTOs;
using StoneBridge.Application.Common.Models;

namespace StoneBridge.Application.Catalog.Queries.SearchCatalogSlabs;

/// <summary>
/// Query: fabricator searches the slab catalog.
/// Only returns slabs from suppliers the fabricator has an ACTIVE connection with.
/// Only returns slabs with status = 'available' and is_active = TRUE.
/// Results are paginated and optionally filtered/sorted.
///
/// Authorization: only fabricator tenants can execute this query.
/// Attempting to call this as a supplier throws ForbiddenException.
/// </summary>
public sealed record SearchCatalogSlabsQuery(CatalogSearchParams SearchParams)
    : IRequest<PagedResult<CatalogSlabDto>>;