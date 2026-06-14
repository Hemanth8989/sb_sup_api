using MediatR;
using Microsoft.Extensions.Logging;
using StoneBridge.Application.Catalog.DTOs;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;

namespace StoneBridge.Application.Catalog.Queries.SearchCatalogSlabs;

/// <summary>
/// Handles SearchCatalogSlabsQuery.
/// Authorization: only fabricator tenants allowed.
/// Delegates all data access to ICatalogRepository.
/// No business logic here beyond the auth check — the repository owns the query.
/// </summary>
public sealed class SearchCatalogSlabsQueryHandler
    : IRequestHandler<SearchCatalogSlabsQuery, PagedResult<CatalogSlabDto>>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly ICurrentTenant     _currentTenant;
    private readonly ILogger<SearchCatalogSlabsQueryHandler> _logger;

    public SearchCatalogSlabsQueryHandler(
        ICatalogRepository                           catalogRepository,
        ICurrentTenant                               currentTenant,
        ILogger<SearchCatalogSlabsQueryHandler>      logger)
    {
        _catalogRepository = catalogRepository;
        _currentTenant     = currentTenant;
        _logger            = logger;
    }

    public async Task<PagedResult<CatalogSlabDto>> Handle(
        SearchCatalogSlabsQuery request,
        CancellationToken       cancellationToken)
    {
        // ── Authorization ──────────────────────────────────────────────────
        // The catalog is a fabricator-only feature.
        // Suppliers manage their own inventory via /api/v1/supplier/slabs (future endpoint).
        if (!_currentTenant.IsFabricator)
        {
            throw new ForbiddenException(
                "The slab catalog is only accessible to fabricator tenants. " +
                "Suppliers should use /api/v1/supplier/slabs to view their own inventory.");
        }

        // ── Execute query ──────────────────────────────────────────────────
        var result = await _catalogRepository.SearchAsync(
            fabricatorId: _currentTenant.TenantId,
            searchParams: request.SearchParams,
            ct:           cancellationToken);

        _logger.LogDebug(
            "Catalog search returned {Count} of {Total} slabs for fabricator {FabricatorId}",
            result.Items.Count,
            result.TotalCount,
            _currentTenant.TenantId);

        return result;
    }
}