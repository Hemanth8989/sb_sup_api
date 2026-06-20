namespace StoneBridge.Application.Notifications.DTOs;

public sealed record NotificationDto
{
    public Guid    Id         { get; init; }
    public string  Type       { get; init; } = string.Empty;
    public string  Title      { get; init; } = string.Empty;
    public string  Body       { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public Guid?   EntityId   { get; init; }
    public string? LinkUrl    { get; init; }
    public bool    IsRead     { get; init; }
    public DateTimeOffset? ReadAt    { get; init; }
    public DateTimeOffset  CreatedAt { get; init; }
}
