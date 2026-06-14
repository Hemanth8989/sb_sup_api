using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Supplier.Bundles.Commands.CreateBundle;
using StoneBridge.Application.Supplier.Bundles.Commands.UpdateBundle;
using StoneBridge.Application.Supplier.Bundles.Queries.GetBundle;
using StoneBridge.Application.Supplier.Bundles.Queries.GetBundles;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Api.Endpoints;

public static class BundleEndpoints
{
    public static void MapBundleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/supplier/bundles")
            .RequireAuthorization()
            .WithTags("Bundles");

        // GET /api/v1/supplier/bundles?search=
        group.MapGet("", async (
            [FromQuery] string? search,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetBundlesQuery(search), ct);
            return Results.Ok(result);
        })
        .WithName("GetBundles")
        .Produces(200);

        // GET /api/v1/supplier/bundles/{id}
        group.MapGet("{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetBundleQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetBundle")
        .Produces(200)
        .Produces(404);

        // POST /api/v1/supplier/bundles
        group.MapPost("", async (
            [FromBody] CreateBundleRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateBundleCommand(body), ct);
            return Results.Created($"/api/v1/supplier/bundles/{result.Id}", result);
        })
        .WithName("CreateBundle")
        .Produces(201)
        .Produces(400);

        // PUT /api/v1/supplier/bundles/{id}
        group.MapPut("{id:guid}", async (
            Guid id,
            [FromBody] UpdateBundleRequest body,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateBundleCommand(id, body), ct);
            return Results.Ok(result);
        })
        .WithName("UpdateBundle")
        .Produces(200)
        .Produces(400)
        .Produces(404);
    }
}
