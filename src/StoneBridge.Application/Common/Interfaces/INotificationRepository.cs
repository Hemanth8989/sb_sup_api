using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Notifications.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task<PagedResult<NotificationDto>> GetAsync(
        Guid tenantId, Guid userId,
        bool? isRead, string? type,
        int page, int perPage,
        CancellationToken ct);

    Task<int> GetUnreadCountAsync(Guid tenantId, Guid userId, CancellationToken ct);

    Task MarkReadAsync(Guid notificationId, Guid tenantId, CancellationToken ct);

    Task MarkAllReadAsync(Guid tenantId, Guid userId, CancellationToken ct);
}
