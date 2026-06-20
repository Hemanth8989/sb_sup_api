using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.Slabs.Commands.BulkUpdateSlabStatus;
using StoneBridge.Application.Supplier.Slabs.Commands.CreateSlab;
using StoneBridge.Application.Supplier.Slabs.Commands.DeleteSlab;
using StoneBridge.Application.Supplier.Slabs.Commands.SetSlabPrice;
using StoneBridge.Application.Supplier.Slabs.Commands.UpdateSlab;
using StoneBridge.Application.Supplier.Slabs.Commands.UpdateSlabStatus;
using StoneBridge.Application.Supplier.Slabs.DTOs;
using StoneBridge.Application.Supplier.Slabs.Queries.GetSlab;
using StoneBridge.Application.Supplier.Slabs.Queries.GetSupplierSlabs;

namespace StoneBridge.Api.Endpoints;

public static class SupplierSlabEndpoints
{
    public static IEndpointRouteBuilder MapSupplierSlabEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/slabs")
            .WithTags("Supplier — Inventory")
            .RequireAuthorization();

        // GET / — paginated inventory list with filters
        group.MapGet("/", async (
            [FromQuery] string?  searchQuery,
            [FromQuery] string?  statuses,
            [FromQuery] string?  materialTypes,
            [FromQuery] string?  colorFamilies,
            [FromQuery] string?  finishes,
            [FromQuery] decimal? thicknessMin,
            [FromQuery] decimal? thicknessMax,
            [FromQuery] decimal? minNetSqft,
            [FromQuery] bool?    isRemnant,
            [FromQuery] Guid?    warehouseId,
            [FromQuery] string   sortBy  = "updatedAt",
            [FromQuery] string   sortDir = "DESC",
            [FromQuery] int      page    = 1,
            [FromQuery] int      perPage = 50,
            ISender sender = default!,
            CancellationToken ct = default) =>
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
            return Results.Ok(result);
        });

        // GET /{id} — single slab detail
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSlabQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // POST / — create slab
        group.MapPost("/", async (CreateSlabRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateSlabCommand(body), ct);
            return Results.Created($"/api/v1/supplier/slabs/{result.Id}", result);
        });

        // PUT /{id} — update slab
        group.MapPut("/{id:guid}", async (Guid id, UpdateSlabRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateSlabCommand(id, body), ct);
            return Results.Ok(result);
        });

        // PATCH /{id}/status — update status
        group.MapPatch("/{id:guid}/status", async (
            Guid id, SlabStatusBody body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateSlabStatusCommand(id, body.Status), ct);
            return Results.Ok(result);
        });

        // PATCH /{id}/price — set price override (null = clear override)
        group.MapPatch("/{id:guid}/price", async (
            Guid id, SlabPriceBody body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetSlabPriceCommand(id, body.PriceOverride), ct);
            return Results.Ok(result);
        });

        // POST /bulk-status — bulk update status for multiple slabs
        group.MapPost("/bulk-status", async (
            BulkUpdateSlabStatusRequest body, ISender sender, CancellationToken ct) =>
        {
            var count = await sender.Send(
                new BulkUpdateSlabStatusCommand(body.SlabIds, body.Status), ct);
            return Results.Ok(new { updated = count });
        });

        // DELETE /{id} — soft-delete (sets is_active = false)
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteSlabCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }

    private static IReadOnlyList<string> SplitCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) { return []; }
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList()
            .AsReadOnly();
    }
}

public sealed record SlabStatusBody(string Status);
public sealed record SlabPriceBody(decimal? PriceOverride);
