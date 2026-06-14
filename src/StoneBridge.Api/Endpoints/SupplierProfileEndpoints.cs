using System.Text.Json.Nodes;
using MediatR;
using StoneBridge.Application.Supplier.Profile.Commands.UpdateNotificationPrefs;
using StoneBridge.Application.Supplier.Profile.Commands.UpdateProfile;
using StoneBridge.Application.Supplier.Profile.DTOs;
using StoneBridge.Application.Supplier.Profile.Queries.GetNotificationPrefs;
using StoneBridge.Application.Supplier.Profile.Queries.GetProfile;
using StoneBridge.Application.Supplier.Profile.Queries.GetProfileStats;

namespace StoneBridge.Api.Endpoints;

/// <summary>
/// Supplier profile endpoints — accessible to supplier tenants only.
/// Fabricators are rejected with 403 at the handler level.
///
/// Base path: /api/v1/supplier/profile
///            /api/v1/supplier/notification-preferences
/// </summary>
public static class SupplierProfileEndpoints
{
    public static IEndpointRouteBuilder MapSupplierProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var profile = app.MapGroup("/api/v1/supplier/profile")
            .WithTags("Supplier — Profile")
            .RequireAuthorization();

        var notifs = app.MapGroup("/api/v1/supplier/notification-preferences")
            .WithTags("Supplier — Profile")
            .RequireAuthorization();

        // ── GET /api/v1/supplier/profile ──────────────────────────────────
        // Returns the supplier's profile. Auto-creates a default row on first call.
        profile.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProfileQuery(), ct);
            return Results.Ok(result);
        })
        .Produces<SupplierProfileDto>()
        .WithSummary("Get supplier profile");

        // ── PUT /api/v1/supplier/profile ──────────────────────────────────
        // Creates or updates the supplier's business profile (upsert).
        profile.MapPut("/", async (
            UpdateProfileRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateProfileCommand(request), ct);
            return Results.Ok(result);
        })
        .Produces<SupplierProfileDto>()
        .WithSummary("Update supplier profile");

        // ── GET /api/v1/supplier/profile/stats ────────────────────────────
        // Returns computed performance metrics (read-only, updated nightly).
        profile.MapGet("/stats", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProfileStatsQuery(), ct);
            return Results.Ok(result);
        })
        .Produces<SupplierStatsDto>()
        .WithSummary("Get supplier performance stats");

        // ── GET /api/v1/supplier/notification-preferences ─────────────────
        notifs.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetNotificationPrefsQuery(), ct);
            return Results.Ok(result);
        })
        .Produces<JsonObject>()
        .WithSummary("Get notification preferences");

        // ── PUT /api/v1/supplier/notification-preferences ─────────────────
        notifs.MapPut("/", async (
            JsonObject prefs,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateNotificationPrefsCommand(prefs), ct);
            return Results.Ok(result);
        })
        .Produces<JsonObject>()
        .WithSummary("Update notification preferences");

        return app;
    }
}
