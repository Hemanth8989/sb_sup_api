using MediatR;

namespace StoneBridge.Application.Notifications.Commands.MarkRead;

public sealed record MarkReadCommand(Guid NotificationId) : IRequest;
