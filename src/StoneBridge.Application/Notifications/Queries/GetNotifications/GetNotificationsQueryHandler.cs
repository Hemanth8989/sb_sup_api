using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Notifications.DTOs;

namespace StoneBridge.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentTenant          _tenant;

    public GetNotificationsQueryHandler(INotificationRepository repo, ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task<PagedResult<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken ct) =>
        _repo.GetAsync(
            _tenant.TenantId, _tenant.UserId,
            request.IsRead, request.Type,
            request.Page, request.PerPage, ct);
}
