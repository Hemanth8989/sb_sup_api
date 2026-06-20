using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Notifications.Queries.GetUnreadCount;

public sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentTenant          _tenant;

    public GetUnreadCountQueryHandler(INotificationRepository repo, ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct) =>
        _repo.GetUnreadCountAsync(_tenant.TenantId, _tenant.UserId, ct);
}
