using MediatR;
using StoneBridge.Application.Supplier.Connections.Commands.AssignPriceList;
using StoneBridge.Application.Supplier.Connections.Commands.RespondConnection;
using StoneBridge.Application.Supplier.Connections.Commands.UpdateConnectionTier;
using StoneBridge.Application.Supplier.Connections.Commands.UpdateNotes;
using StoneBridge.Application.Supplier.Connections.DTOs;
using StoneBridge.Application.Supplier.Connections.Queries.GetConnection;
using StoneBridge.Application.Supplier.Connections.Queries.GetConnections;

namespace StoneBridge.Api.Endpoints;

public static class ConnectionEndpoints
{
    public static void MapConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/connections")
            .WithTags("Connections")
            .RequireAuthorization();

        group.MapGet("/", async (
            string?           status,
            ISender           sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetConnectionsQuery(status), ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetConnectionQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/{id:guid}/respond", async (
            Guid                      id,
            RespondConnectionRequest  body,
            ISender                   sender,
            CancellationToken         ct) =>
        {
            var result = await sender.Send(
                new RespondConnectionCommand(id, body.Action, body.Reason), ct);
            return Results.Ok(result);
        });

        group.MapPatch("/{id:guid}/tier", async (
            Guid                         id,
            UpdateConnectionTierRequest  body,
            ISender                      sender,
            CancellationToken            ct) =>
        {
            var result = await sender.Send(new UpdateConnectionTierCommand(id, body.PricingTier), ct);
            return Results.Ok(result);
        });

        group.MapPatch("/{id:guid}/notes", async (
            Guid                          id,
            UpdateConnectionNotesRequest  body,
            ISender                       sender,
            CancellationToken             ct) =>
        {
            var result = await sender.Send(new UpdateNotesCommand(id, body.FabricatorNotes), ct);
            return Results.Ok(result);
        });

        group.MapPatch("/{id:guid}/price-list", async (
            Guid              id,
            AssignPriceListBody body,
            ISender           sender,
            CancellationToken ct) =>
        {
            await sender.Send(new AssignPriceListCommand(id, body.PriceListId), ct);
            return Results.NoContent();
        });
    }
}

public sealed record AssignPriceListBody(Guid? PriceListId);
