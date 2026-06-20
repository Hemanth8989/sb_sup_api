using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Supplier.Warehouses.Commands.CreateWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Commands.DeactivateWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Commands.SetPrimaryWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Commands.TransferSlabs;
using StoneBridge.Application.Supplier.Warehouses.Commands.UpdateWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Commands.ReceiveWarehouseStock;
using StoneBridge.Application.Supplier.Warehouses.Commands.TransferWarehouseStock;
using StoneBridge.Application.Supplier.Warehouses.Commands.AdjustWarehouseStock;
using StoneBridge.Application.Supplier.Warehouses.Commands.SetStockReorderPoint;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouses;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseProductStock;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetStockMovements;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseAuditLog;
using StoneBridge.Application.Supplier.Warehouses.Queries.ExportWarehouseSlabs;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseBundles;
using StoneBridge.Application.Supplier.Warehouses.DTOs;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static void MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/supplier/warehouses")
            .RequireAuthorization()
            .WithTags("Warehouses");

        // GET /api/v1/supplier/warehouses
        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWarehousesQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetWarehouses")
        .Produces(200);

        // GET /api/v1/supplier/warehouses/{id}
        group.MapGet("{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWarehouseQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetWarehouse")
        .Produces(200)
        .Produces(404);

        // POST /api/v1/supplier/warehouses
        group.MapPost("", async (
            [FromBody] CreateWarehouseRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateWarehouseCommand(body), ct);
            return Results.Created($"/api/v1/supplier/warehouses/{result.Id}", result);
        })
        .WithName("CreateWarehouse")
        .Produces(201)
        .Produces(400);

        // PUT /api/v1/supplier/warehouses/{id}
        group.MapPut("{id:guid}", async (
            Guid id,
            [FromBody] UpdateWarehouseRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateWarehouseCommand(id, body), ct);
            return Results.Ok(result);
        })
        .WithName("UpdateWarehouse")
        .Produces(200)
        .Produces(400)
        .Produces(404);

        // PATCH /api/v1/supplier/warehouses/{id}/set-primary
        group.MapPatch("{id:guid}/set-primary", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetPrimaryWarehouseCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("SetPrimaryWarehouse")
        .Produces(204)
        .Produces(404);

        // DELETE /api/v1/supplier/warehouses/{id}
        group.MapDelete("{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeactivateWarehouseCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeactivateWarehouse")
        .Produces(204)
        .Produces(404);

        // POST /api/v1/supplier/warehouses/{id}/transfer-slabs
        group.MapPost("{id:guid}/transfer-slabs", async (
            Guid id,
            [FromBody] TransferSlabsRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var count = await sender.Send(new TransferSlabsCommand(id, body), ct);
            return Results.Ok(new { TransferredCount = count });
        })
        .WithName("TransferSlabs")
        .Produces(200)
        .Produces(400)
        .Produces(404);

        // ── Product stock endpoints ─────────────────────────────────────────

        // GET /api/v1/supplier/warehouses/{id}/products
        group.MapGet("{id:guid}/products", async (
            Guid id,
            [AsParameters] WarehouseProductStockFilterParams filter,
            ISender sender, CancellationToken ct) =>
        {
            var (items, total) = await sender.Send(new GetWarehouseProductStockQuery(id, filter), ct);
            return Results.Ok(new { Items = items, TotalCount = total });
        })
        .WithName("GetWarehouseProducts")
        .Produces(200);

        // POST /api/v1/supplier/warehouses/{id}/products/receive
        group.MapPost("{id:guid}/products/receive", async (
            Guid id,
            [FromBody] ReceiveStockRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReceiveWarehouseStockCommand(id, body), ct);
            return Results.Ok(result);
        })
        .WithName("ReceiveWarehouseStock")
        .Produces(200)
        .Produces(400);

        // POST /api/v1/supplier/warehouses/{id}/products/transfer
        group.MapPost("{id:guid}/products/transfer", async (
            Guid id,
            [FromBody] TransferWarehouseStockRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var (fromQty, toQty) = await sender.Send(new TransferWarehouseStockCommand(id, body), ct);
            return Results.Ok(new { FromWarehouseQty = fromQty, ToWarehouseQty = toQty });
        })
        .WithName("TransferWarehouseStock")
        .Produces(200)
        .Produces(400);

        // PATCH /api/v1/supplier/warehouses/{id}/products/adjust
        group.MapPatch("{id:guid}/products/adjust", async (
            Guid id,
            [FromBody] AdjustWarehouseStockRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AdjustWarehouseStockCommand(id, body), ct);
            return Results.Ok(result);
        })
        .WithName("AdjustWarehouseStock")
        .Produces(200)
        .Produces(400);

        // PATCH /api/v1/supplier/warehouses/{id}/products/reorder
        group.MapPatch("{id:guid}/products/reorder", async (
            Guid id,
            [FromBody] SetReorderPointRequest body,
            ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SetStockReorderPointCommand(id, body), ct);
            return Results.NoContent();
        })
        .WithName("SetStockReorderPoint")
        .Produces(204)
        .Produces(400);

        // GET /api/v1/supplier/warehouses/{id}/stock-movements
        group.MapGet("{id:guid}/stock-movements", async (
            Guid id,
            [FromQuery] int limit = 100,
            ISender sender = default!,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetStockMovementsQuery(id, limit), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockMovements")
        .Produces(200);

        // GET /api/v1/supplier/warehouses/{id}/history
        group.MapGet("{id:guid}/history", async (
            Guid id,
            [FromQuery] int limit = 200,
            ISender sender = default!,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetWarehouseAuditLogQuery(id, limit), ct);
            return Results.Ok(result);
        })
        .WithName("GetWarehouseHistory")
        .Produces(200);

        // GET /api/v1/supplier/warehouses/{id}/bundles
        group.MapGet("{id:guid}/bundles", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWarehouseBundlesQuery(id), ct);
            return Results.Ok(result);
        })
        .WithName("GetWarehouseBundles")
        .Produces(200);

        // GET /api/v1/supplier/warehouses/{id}/export
        group.MapGet("{id:guid}/export", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var csv = await sender.Send(new ExportWarehouseSlabsQuery(id), ct);
            return Results.Text(csv, "text/csv");
        })
        .WithName("ExportWarehouseSlabs")
        .Produces(200);
    }
}
