using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Notifications.Commands.MarkRead;

public sealed class MarkReadCommandHandler : IRequestHandler<MarkReadCommand>
{
    private readonly INotificationRepository _repo;
    private readonly ICurrentTenant          _tenant;

    public MarkReadCommandHandler(INotificationRepository repo, ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task Handle(MarkReadCommand request, CancellationToken ct) =>
        _repo.MarkReadAsync(request.NotificationId, _tenant.TenantId, ct);
}
