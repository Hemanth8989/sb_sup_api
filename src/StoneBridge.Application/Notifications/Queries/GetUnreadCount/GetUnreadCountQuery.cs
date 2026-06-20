using MediatR;

namespace StoneBridge.Application.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<int>;
