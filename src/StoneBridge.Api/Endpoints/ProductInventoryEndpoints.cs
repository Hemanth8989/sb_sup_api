using MediatR;
using StoneBridge.Application.Supplier.ProductInventory.Commands.AdjustStock;
using StoneBridge.Application.Supplier.ProductInventory.Commands.DeactivateProduct;
using StoneBridge.Application.Supplier.ProductInventory.Commands.UpsertProduct;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;
using StoneBridge.Application.Supplier.ProductInventory.Queries.GetProduct;
using StoneBridge.Application.Supplier.ProductInventory.Queries.GetProducts;

namespace StoneBridge.Api.Endpoints;

public static class ProductInventoryEndpoints
{
    public static void MapProductInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/products")
            .WithTags("ProductInventory")
            .RequireAuthorization();

        group.MapGet("/", async (
            string?           categoryCode,
            string?           search,
            string?           status,
            int?              page,
            int?              perPage,
            ISender           sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProductsQuery(
                new ProductInventoryFilterParams(
                    categoryCode,
                    search,
                    status,
                    page    ?? 1,
                    perPage ?? 40)), ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProductQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (
            UpsertProductCommand body,
            ISender              sender,
            CancellationToken    ct) =>
        {
            var result = await sender.Send(body, ct);
            return Results.Ok(result);
        });

        group.MapPatch("/{variantId:guid}/stock", async (
            Guid              variantId,
            AdjustStockBody   body,
            ISender           sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AdjustStockCommand(variantId, body.Delta, body.NewPrice, body.Reason), ct);
            return Results.Ok(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeactivateProductCommand(id), ct);
            return Results.NoContent();
        });
    }
}

public sealed record AdjustStockBody(int Delta, decimal? NewPrice, string? Reason);
