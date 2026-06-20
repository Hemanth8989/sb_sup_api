using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Notifications.Commands.MarkAllRead;

public sealed class MarkAllReadCommandHandler : IRequestHandler<MarkAllReadCommand>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentTenant          _tenant;

    public MarkAllReadCommandHandler(INotificationRepository repo, ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task Handle(MarkAllReadCommand request, CancellationToken ct) =>
        _repo.MarkAllReadAsync(_tenant.TenantId, _tenant.UserId, ct);
}
