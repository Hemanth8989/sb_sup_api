using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Catalog.DTOs;
using StoneBridge.Application.Catalog.Queries.SearchCatalogSlabs;
using StoneBridge.Application.Common.Models;

namespace StoneBridge.Api.Endpoints;

/// <summary>
/// Catalog endpoints — accessible to fabricator tenants only.
/// Suppliers are rejected with 403 at the handler level.
///
/// Base path: /api/v1/catalog
/// </summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog")
            .WithTags("Catalog")
            .RequireAuthorization();

        // ── GET /api/v1/catalog/slabs ─────────────────────────────────────
        // Fabricator searches available slabs from their connected suppliers.
        // All filters are optional — omitting all filters returns all accessible slabs.
        group.MapGet("/slabs", async (
            // Full-text search
            [FromQuery] string? searchQuery,

            // Multi-value filters (comma-separated in query string, split by endpoint)
            [FromQuery] string? materialTypes,
            [FromQuery] string? colorFamilies,
            [FromQuery] string? finishes,

            // Range filters
            [FromQuery] decimal? thicknessMin,
            [FromQuery] decimal? thicknessMax,
            [FromQuery] decimal? priceMin,
            [FromQuery] decimal? priceMax,
            [FromQuery] decimal? minNetSqft,

            // Boolean filter
            [FromQuery] bool? isRemnant,

            // Supplier filter
            [FromQuery] Guid? supplierId,

            // Sorting
            [FromQuery] string sortBy  = "updatedAt",
            [FromQuery] string sortDir = "DESC",

            // Pagination
            [FromQuery] int page    = 1,
            [FromQuery] int perPage = 24,

            // MediatR sender (injected by DI)
            ISender           sender = default!,
            CancellationToken ct     = default) =>
        {
            // Parse comma-separated multi-value query parameters
            // Example: ?materialTypes=marble,granite,quartzite
            var searchParams = new CatalogSearchParams
            {
                SearchQuery    = searchQuery?.Trim(),
                MaterialTypes  = SplitCsv(materialTypes),
                ColorFamilies  = SplitCsv(colorFamilies),
                Finishes       = SplitCsv(finishes),
                ThicknessMinCm = thicknessMin,
                ThicknessMaxCm = thicknessMax,
                PriceMin       = priceMin,
                PriceMax       = priceMax,
                MinNetSqft     = minNetSqft,
                IsRemnant      = isRemnant,
                SupplierId     = supplierId,
                SortBy         = sortBy,
                SortDir        = sortDir,
                Page           = Math.Max(1, page),
                PerPage        = Math.Clamp(perPage, 1, 100),
            };

            var result = await sender.Send(new SearchCatalogSlabsQuery(searchParams), ct);

            return Results.Ok(ApiResponse<PagedResult<CatalogSlabDto>>.Ok(
                result,
                ApiMeta.From(result)));
        })
        .WithName("SearchCatalogSlabs")
        .WithSummary("Search the slab catalog. Returns available slabs from connected suppliers.")
        .WithDescription("""
            Fabricator-only endpoint. Returns slabs from suppliers the fabricator has an active
            connection with. All filter parameters are optional. Supports full-text search,
            multi-value material/colour/finish filters, thickness/price/sqft range filters,
            and remnant flag filtering.

            Multi-value parameters accept comma-separated values:
              ?materialTypes=marble,granite
              ?colorFamilies=white,cream
              ?finishes=polished,honed

            Sort options: updatedAt (default), listPrice, netSqft
            Sort directions: DESC (default), ASC
            """)
        .Produces<ApiResponse<PagedResult<CatalogSlabDto>>>(StatusCodes.Status200OK)
        .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static IReadOnlyList<string> SplitCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList()
            .AsReadOnly();
    }
}