using MediatR;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Notifications.Commands.MarkAllRead;
using StoneBridge.Application.Notifications.Commands.MarkRead;
using StoneBridge.Application.Notifications.DTOs;
using StoneBridge.Application.Notifications.Queries.GetNotifications;
using StoneBridge.Application.Notifications.Queries.GetUnreadCount;

namespace StoneBridge.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        // GET /notifications
        group.MapGet("/", async (
            [FromQuery] bool?   isRead,
            [FromQuery] string? type,
            [FromQuery] int     page    = 1,
            [FromQuery] int     perPage = 30,
            ISender sender = default!,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetNotificationsQuery(isRead, type, Math.Max(1, page), Math.Clamp(perPage, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetNotifications")
        .WithSummary("Paginated notification inbox. Filter by isRead or type.")
        .Produces<PagedResult<NotificationDto>>();

        // GET /notifications/unread-count
        group.MapGet("/unread-count", async (ISender sender, CancellationToken ct) =>
        {
            var count = await sender.Send(new GetUnreadCountQuery(), ct);
            return Results.Ok(new { count });
        })
        .WithName("GetUnreadCount")
        .WithSummary("Unread notification count — use for sidebar badge.");

        // PATCH /notifications/{id}/read
        group.MapPatch("/{id:guid}/read", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new MarkReadCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("MarkNotificationRead")
        .WithSummary("Mark a single notification as read.");

        // POST /notifications/read-all
        group.MapPost("/read-all", async (ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new MarkAllReadCommand(), ct);
            return Results.NoContent();
        })
        .WithName("MarkAllNotificationsRead")
        .WithSummary("Mark all notifications as read for the current user.");

        return app;
    }
}
