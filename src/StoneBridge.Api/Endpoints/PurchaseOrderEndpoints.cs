using MediatR;
using StoneBridge.Application.Supplier.PurchaseOrders.Commands.AcknowledgePo;
using StoneBridge.Application.Supplier.PurchaseOrders.Commands.CancelPo;
using StoneBridge.Application.Supplier.PurchaseOrders.Commands.ShipPo;
using StoneBridge.Application.Supplier.PurchaseOrders.Commands.UpdatePoNotes;
using StoneBridge.Application.Supplier.PurchaseOrders.Commands.UpdatePoStatus;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;
using StoneBridge.Application.Supplier.PurchaseOrders.Queries.GetPurchaseOrder;
using StoneBridge.Application.Supplier.PurchaseOrders.Queries.GetPurchaseOrders;

namespace StoneBridge.Api.Endpoints;

public static class PurchaseOrderEndpoints
{
    public static void MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/purchase-orders")
            .WithTags("PurchaseOrders")
            .RequireAuthorization();

        group.MapGet("/", async (
            string?  status,
            string?  search,
            int?     page,
            int?     perPage,
            ISender  sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetPurchaseOrdersQuery(new PoFilterParams(status, search, page ?? 1, perPage ?? 25)),
                ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPurchaseOrderQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/{id:guid}/acknowledge", async (
            Guid               id,
            AcknowledgePoRequest body,
            ISender            sender,
            CancellationToken  ct) =>
        {
            var result = await sender.Send(new AcknowledgePoCommand(id, body), ct);
            return Results.Ok(result);
        });

        group.MapPost("/{id:guid}/ship", async (
            Guid              id,
            ShipPoRequest     body,
            ISender           sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ShipPoCommand(id, body), ct);
            return Results.Ok(result);
        });

        group.MapPost("/{id:guid}/cancel", async (
            Guid              id,
            CancelPoBody      body,
            ISender           sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CancelPoCommand(id, body.Reason), ct);
            return Results.Ok(result);
        });

        group.MapPatch("/{id:guid}/notes", async (
            Guid              id,
            UpdatePoNotesBody body,
            ISender           sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdatePoNotesCommand(id, body.SupplierNotes), ct);
            return Results.Ok(result);
        });

        group.MapPatch("/{id:guid}/status", async (
            Guid                  id,
            UpdatePoStatusRequest body,
            ISender               sender,
            CancellationToken     ct) =>
        {
            var result = await sender.Send(new UpdatePoStatusCommand(id, body), ct);
            return Results.Ok(result);
        });
    }
}

public sealed record CancelPoBody(string? Reason);
public sealed record UpdatePoNotesBody(string? SupplierNotes);
