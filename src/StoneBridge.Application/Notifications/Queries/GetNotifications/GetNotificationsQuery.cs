using MediatR;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Notifications.DTOs;

namespace StoneBridge.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(
    bool?  IsRead  = null,
    string? Type   = null,
    int    Page    = 1,
    int    PerPage = 30
) : IRequest<PagedResult<NotificationDto>>;
