using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.Slabs.DTOs;
using StoneBridge.Application.Supplier.Slabs.Queries.GetSupplierSlabs;

namespace StoneBridge.Api.Endpoints;

/// <summary>
/// Supplier inventory endpoints — accessible to supplier tenants only.
/// Fabricators are rejected with 403 at the handler level.
///
/// Base path: /api/v1/supplier/slabs
/// </summary>
public static class SupplierSlabEndpoints
{
    public static IEndpointRouteBuilder MapSupplierSlabEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/slabs")
            .WithTags("Supplier — Inventory")
            .RequireAuthorization();

        // ── GET /api/v1/supplier/slabs ────────────────────────────────────────
        // Returns the supplier's own slab inventory — all statuses by default.
        // Filter by status to get just available stock, reserved slabs, etc.
        group.MapGet("/", async (
            // Full-text search
            [FromQuery] string? searchQuery,

            // Status filter (comma-separated): available,reserved,allocated,shipped,hold,sold
            [FromQuery] string? statuses,

            // Multi-value material filters (comma-separated)
            [FromQuery] string? materialTypes,
            [FromQuery] string? colorFamilies,
            [FromQuery] string? finishes,

            // Range filters
            [FromQuery] decimal? thicknessMin,
            [FromQuery] decimal? thicknessMax,
            [FromQuery] decimal? minNetSqft,

            // Boolean filter
            [FromQuery] bool? isRemnant,

            // Location filter
            [FromQuery] Guid? warehouseId,

            // Sorting
            [FromQuery] string sortBy  = "updatedAt",
            [FromQuery] string sortDir = "DESC",

            // Pagination
            [FromQuery] int page    = 1,
            [FromQuery] int perPage = 50,

            ISender           sender = default!,
            CancellationToken ct     = default) =>
        {
            var filterParams = new SupplierSlabFilterParams
            {
                SearchQuery    = searchQuery?.Trim(),
                Statuses       = SplitCsv(statuses),
                MaterialTypes  = SplitCsv(materialTypes),
                ColorFamilies  = SplitCsv(colorFamilies),
                Finishes       = SplitCsv(finishes),
                ThicknessMinCm = thicknessMin,
                ThicknessMaxCm = thicknessMax,
                MinNetSqft     = minNetSqft,
                IsRemnant      = isRemnant,
                WarehouseId    = warehouseId,
                SortBy         = sortBy,
                SortDir        = sortDir,
                Page           = Math.Max(1, page),
                PerPage        = Math.Clamp(perPage, 1, 100),
            };

            var result = await sender.Send(new GetSupplierSlabsQuery(filterParams), ct);

            return Results.Ok(ApiResponse<PagedResult<SupplierSlabDto>>.Ok(
                result,
                ApiMeta.From(result)));
        })
        .WithName("GetSupplierInventory")
        .WithSummary("Get the supplier's slab inventory.")
        .WithDescription("""
            Supplier-only endpoint. Returns all slabs belonging to the authenticated supplier.
            Unlike the fabricator catalog, this includes slabs in ALL statuses so suppliers
            can see their complete inventory picture.

            Filter by status to narrow the view:
              ?statuses=available               → only available stock
              ?statuses=available,reserved      → available + reserved
              ?statuses=reserved,allocated      → slabs committed to purchase orders

            Multi-value parameters accept comma-separated values:
              ?materialTypes=marble,granite
              ?colorFamilies=white,cream
              ?finishes=polished,honed

            Sort options: updatedAt (default), status, internalRef, netSqft, createdAt
            Sort directions: DESC (default), ASC
            """)
        .Produces<ApiResponse<PagedResult<SupplierSlabDto>>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

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
