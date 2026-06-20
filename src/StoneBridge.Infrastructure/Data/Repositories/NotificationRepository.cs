using System.Text;
using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Notifications.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _db;

    public NotificationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<NotificationDto>> GetAsync(
        Guid tenantId, Guid userId,
        bool? isRead, string? type,
        int page, int perPage,
        CancellationToken ct)
    {
        var offset = (page - 1) * perPage;
        var where  = BuildWhere(isRead, type);

        var countSql = $"SELECT COUNT(*) FROM notifications {where}";
        var dataSql  = $"""
            SELECT id, type, title, body, entity_type, entity_id,
                   link_url, is_read, read_at, created_at
            FROM   notifications
            {where}
            ORDER  BY created_at DESC
            LIMIT  @PerPage OFFSET @Offset
            """;

        var p = new { TenantId = tenantId, UserId = userId, IsRead = isRead, Type = type, PerPage = perPage, Offset = offset };

        using var conn = await _db.CreateConnectionAsync(ct);
        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        if (total == 0) { return PagedResult<NotificationDto>.Empty(page, perPage); }

        var rows = await conn.QueryAsync<NotificationDto>(dataSql, p);
        return PagedResult<NotificationDto>.Create(rows, total, page, perPage);
    }

    public async Task<int> GetUnreadCountAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM   notifications
            WHERE  tenant_id = @TenantId
              AND  (user_id = @UserId OR user_id IS NULL)
              AND  is_read   = FALSE
            """;
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, UserId = userId });
    }

    public async Task MarkReadAsync(Guid notificationId, Guid tenantId, CancellationToken ct)
    {
        const string sql = """
            UPDATE notifications
            SET    is_read = TRUE, read_at = NOW()
            WHERE  id = @Id AND tenant_id = @TenantId AND is_read = FALSE
            """;
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = notificationId, TenantId = tenantId });
    }

    public async Task MarkAllReadAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        const string sql = """
            UPDATE notifications
            SET    is_read = TRUE, read_at = NOW()
            WHERE  tenant_id = @TenantId
              AND  (user_id = @UserId OR user_id IS NULL)
              AND  is_read   = FALSE
            """;
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { TenantId = tenantId, UserId = userId });
    }

    private static string BuildWhere(bool? isRead, string? type)
    {
        var sb = new StringBuilder("WHERE tenant_id = @TenantId AND (user_id = @UserId OR user_id IS NULL)");
        if (isRead.HasValue)             { sb.Append(" AND is_read = @IsRead"); }
        if (!string.IsNullOrEmpty(type)) { sb.Append(" AND type = @Type"); }
        return sb.ToString();
    }
}
