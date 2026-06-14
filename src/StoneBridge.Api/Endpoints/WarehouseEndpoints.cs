using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Supplier.Warehouses.Commands.CreateWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Commands.DeactivateWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Commands.SetPrimaryWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Commands.TransferSlabs;
using StoneBridge.Application.Supplier.Warehouses.Commands.UpdateWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouse;
using StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouses;
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
    }
}
