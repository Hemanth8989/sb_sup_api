using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class ConnectionRepository : IConnectionRepository
{
    private readonly IDbConnectionFactory _db;

    public ConnectionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<ConnectionDto>> GetAllAsync(
        Guid supplierId, string? status, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                c.id, c.fabricator_id, c.supplier_id, c.status, c.pricing_tier,
                c.request_message, c.decline_reason, c.fabricator_notes,
                c.requested_at, c.connected_at, c.suspended_at, c.terminated_at,
                c.updated_at,
                t.name   AS fabricator_name,
                t.slug   AS fabricator_slug,
                fp.city  AS fabricator_city,
                fp.state_province AS fabricator_state,
                fp.phone AS fabricator_phone,
                cpl.price_list_id::TEXT AS assigned_price_list_id,
                pl.name                 AS assigned_price_list_name
            FROM connections c
            JOIN tenants t ON t.id = c.fabricator_id
            LEFT JOIN fabricator_profiles fp ON fp.tenant_id = c.fabricator_id
            LEFT JOIN LATERAL (
                SELECT price_list_id
                FROM connection_price_lists
                WHERE connection_id = c.id
                ORDER BY assigned_at DESC
                LIMIT 1
            ) cpl ON TRUE
            LEFT JOIN price_lists pl ON pl.id = cpl.price_list_id
            WHERE c.supplier_id = @SupplierId
              AND (@Status IS NULL OR c.status = @Status)
            ORDER BY c.updated_at DESC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<dynamic>(sql, new { SupplierId = supplierId, Status = status });
        return rows.Select(MapConnection).ToList();
    }

    public async Task<ConnectionDto?> GetByIdAsync(
        Guid supplierId, Guid connectionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                c.id, c.fabricator_id, c.supplier_id, c.status, c.pricing_tier,
                c.request_message, c.decline_reason, c.fabricator_notes,
                c.requested_at, c.connected_at, c.suspended_at, c.terminated_at,
                c.updated_at,
                t.name   AS fabricator_name,
                t.slug   AS fabricator_slug,
                fp.city  AS fabricator_city,
                fp.state_province AS fabricator_state,
                fp.phone AS fabricator_phone,
                cpl.price_list_id::TEXT AS assigned_price_list_id,
                pl.name                 AS assigned_price_list_name
            FROM connections c
            JOIN tenants t ON t.id = c.fabricator_id
            LEFT JOIN fabricator_profiles fp ON fp.tenant_id = c.fabricator_id
            LEFT JOIN LATERAL (
                SELECT price_list_id
                FROM connection_price_lists
                WHERE connection_id = c.id
                ORDER BY assigned_at DESC
                LIMIT 1
            ) cpl ON TRUE
            LEFT JOIN price_lists pl ON pl.id = cpl.price_list_id
            WHERE c.id = @ConnectionId AND c.supplier_id = @SupplierId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { ConnectionId = connectionId, SupplierId = supplierId });
        return row is null ? null : MapConnection(row);
    }

    public async Task<ConnectionDto> RespondAsync(
        Guid supplierId, Guid connectionId, string action, string? reason, CancellationToken ct = default)
    {
        var (newStatus, timestampCol) = action switch
        {
            "approve"    => ("active",      "connected_at"),
            "decline"    => ("declined",    null as string),
            "suspend"    => ("suspended",   "suspended_at"),
            "terminate"  => ("terminated",  "terminated_at"),
            "reactivate" => ("active",      "connected_at"),
            _            => throw new ArgumentException($"Unknown action: {action}")
        };

        var tsUpdate = timestampCol is null ? "" : $", {timestampCol} = NOW()";

        var sql = $"""
            UPDATE connections
            SET status         = @Status
                {tsUpdate}
                {(action == "decline" ? ", decline_reason = @Reason" : "")}
                {(action is "suspend" or "terminate" ? ", decline_reason = @Reason" : "")},
                updated_at     = NOW()
            WHERE id = @ConnectionId AND supplier_id = @SupplierId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Status = newStatus, ConnectionId = connectionId, SupplierId = supplierId, Reason = reason });

        return (await GetByIdAsync(supplierId, connectionId, ct))!;
    }

    public async Task<ConnectionDto> UpdateTierAsync(
        Guid supplierId, Guid connectionId, string tier, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE connections
            SET pricing_tier = @Tier, updated_at = NOW()
            WHERE id = @ConnectionId AND supplier_id = @SupplierId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Tier = tier, ConnectionId = connectionId, SupplierId = supplierId });

        return (await GetByIdAsync(supplierId, connectionId, ct))!;
    }

    public async Task<ConnectionDto> UpdateNotesAsync(
        Guid supplierId, Guid connectionId, string? notes, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE connections
            SET fabricator_notes = @Notes, updated_at = NOW()
            WHERE id = @ConnectionId AND supplier_id = @SupplierId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Notes = notes, ConnectionId = connectionId, SupplierId = supplierId });

        return (await GetByIdAsync(supplierId, connectionId, ct))!;
    }

    public async Task AssignPriceListAsync(
        Guid supplierId, Guid connectionId, Guid priceListId, Guid assignedBy, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO connection_price_lists (connection_id, price_list_id, assigned_by)
            VALUES (@ConnectionId, @PriceListId, @AssignedBy)
            ON CONFLICT DO NOTHING
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { ConnectionId = connectionId, PriceListId = priceListId, AssignedBy = assignedBy });
    }

    public async Task RemovePriceListAsync(Guid supplierId, Guid connectionId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM connection_price_lists WHERE connection_id = @ConnectionId";
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { ConnectionId = connectionId });
    }

    private static ConnectionDto MapConnection(dynamic r) => new()
    {
        Id                    = r.id,
        FabricatorId          = r.fabricator_id,
        FabricatorName        = r.fabricator_name ?? string.Empty,
        FabricatorSlug        = r.fabricator_slug ?? string.Empty,
        SupplierId            = r.supplier_id,
        Status                = r.status,
        PricingTier           = r.pricing_tier,
        RequestMessage        = r.request_message,
        DeclineReason         = r.decline_reason,
        FabricatorNotes       = r.fabricator_notes,
        RequestedAt           = r.requested_at,
        ConnectedAt           = r.connected_at,
        SuspendedAt           = r.suspended_at,
        TerminatedAt          = r.terminated_at,
        UpdatedAt             = r.updated_at,
        FabricatorCity        = r.fabricator_city,
        FabricatorState       = r.fabricator_state,
        FabricatorPhone       = r.fabricator_phone,
        AssignedPriceListId   = r.assigned_price_list_id,
        AssignedPriceListName = r.assigned_price_list_name
    };
}
