using MediatR;
using StoneBridge.Application.Supplier.PriceLists.Commands.CreatePriceList;
using StoneBridge.Application.Supplier.PriceLists.Commands.DeletePriceList;
using StoneBridge.Application.Supplier.PriceLists.Commands.ClonePriceList;
using StoneBridge.Application.Supplier.PriceLists.Commands.RemovePriceListItem;
using StoneBridge.Application.Supplier.PriceLists.Commands.UpdatePriceList;
using StoneBridge.Application.Supplier.PriceLists.Commands.UpsertPriceListItem;
using StoneBridge.Application.Supplier.PriceLists.Queries.GetPriceList;
using StoneBridge.Application.Supplier.PriceLists.Queries.GetPriceLists;

namespace StoneBridge.Api.Endpoints;

public static class PriceListEndpoints
{
    public static void MapPriceListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/price-lists")
            .WithTags("PriceLists")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPriceListsQuery(), ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPriceListQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (
            CreatePriceListCommand body,
            ISender                sender,
            CancellationToken      ct) =>
        {
            var result = await sender.Send(body, ct);
            return Results.Created($"/api/v1/supplier/price-lists/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (
            Guid                   id,
            UpdatePriceListBody    body,
            ISender                sender,
            CancellationToken      ct) =>
        {
            var result = await sender.Send(
                new UpdatePriceListCommand(id, body.Name, body.Description, body.Tier, body.Currency, body.IsActive),
                ct);
            return Results.Ok(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeletePriceListCommand(id), ct);
            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/items", async (
            Guid                      id,
            UpsertPriceListItemBody   body,
            ISender                   sender,
            CancellationToken         ct) =>
        {
            var result = await sender.Send(
                new UpsertPriceListItemCommand(id, body.VariantId, body.UnitPrice), ct);
            return Results.Ok(result);
        });

        group.MapPost("/{id:guid}/clone", async (
            Guid              id,
            ClonePriceListBody body,
            ISender           sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ClonePriceListCommand(id, body.Name), ct);
            return Results.Created($"/api/v1/supplier/price-lists/{result.Id}", result);
        })
        .WithName("ClonePriceList")
        .Produces(201)
        .Produces(404);

        group.MapDelete("/{id:guid}/items/{itemId:guid}", async (
            Guid              id,
            Guid              itemId,
            ISender           sender,
            CancellationToken ct) =>
        {
            await sender.Send(new RemovePriceListItemCommand(id, itemId), ct);
            return Results.NoContent();
        });
    }
}

public sealed record UpdatePriceListBody(
    string  Name,
    string? Description,
    string  Tier,
    string  Currency,
    bool    IsActive);

public sealed record UpsertPriceListItemBody(Guid VariantId, decimal UnitPrice);
public sealed record ClonePriceListBody(string Name);
