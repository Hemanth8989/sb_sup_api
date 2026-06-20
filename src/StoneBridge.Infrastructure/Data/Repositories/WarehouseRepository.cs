using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly IDbConnectionFactory _db;

    public WarehouseRepository(IDbConnectionFactory db) => _db = db;

    // ── List with live slab + product stock stats ─────────────────────────
    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                w.id, w.name, w.address_line1, w.city, w.state_province,
                w.postal_code, w.country, w.phone,
                w.is_primary, w.is_active, w.created_at, w.updated_at,
                COALESCE(sagg.slab_count,      0)   AS slab_count,
                COALESCE(sagg.available_count, 0)   AS available_count,
                COALESCE(sagg.reserved_count,  0)   AS reserved_count,
                COALESCE(sagg.on_hold_count,   0)   AS on_hold_count,
                sagg.estimated_value,
                COALESCE(pagg.product_sku_count, 0) AS product_sku_count,
                COALESCE(pagg.low_stock_count,   0) AS low_stock_count,
                pagg.product_stock_value
            FROM warehouses w
            LEFT JOIN (
                SELECT
                    s.warehouse_id,
                    COUNT(*)                                                               AS slab_count,
                    COUNT(*) FILTER (WHERE s.status = 'available')                        AS available_count,
                    COUNT(*) FILTER (WHERE s.status = 'reserved')                         AS reserved_count,
                    COUNT(*) FILTER (WHERE s.status = 'hold')                             AS on_hold_count,
                    ROUND(SUM(s.net_sqft * COALESCE(s.price_override, pv.base_price, 0))
                          FILTER (WHERE s.status = 'available'), 2)                       AS estimated_value
                FROM   slabs s
                JOIN   product_variants pv ON pv.id = s.variant_id
                WHERE  s.is_active = TRUE AND s.tenant_id = @tenantId
                GROUP BY s.warehouse_id
            ) sagg ON sagg.warehouse_id = w.id
            LEFT JOIN (
                SELECT
                    wps.warehouse_id,
                    COUNT(*)                                                                     AS product_sku_count,
                    COUNT(*) FILTER (
                        WHERE wps.reorder_point IS NOT NULL AND wps.qty_on_hand <= wps.reorder_point
                    )                                                                            AS low_stock_count,
                    ROUND(SUM(wps.qty_on_hand * pv2.base_price), 2)                             AS product_stock_value
                FROM   warehouse_product_stock wps
                JOIN   product_variants pv2 ON pv2.id = wps.variant_id
                WHERE  wps.tenant_id = @tenantId AND pv2.is_slab_variant = FALSE
                GROUP BY wps.warehouse_id
            ) pagg ON pagg.warehouse_id = w.id
            WHERE  w.tenant_id = @tenantId AND w.is_active = TRUE
            ORDER BY w.is_primary DESC, w.name ASC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<WarehouseRow>(sql, new { tenantId });
        return rows.Select(MapToDto).ToList();
    }

    // ── Single warehouse ───────────────────────────────────────────────────
    public async Task<WarehouseDto?> GetByIdAsync(
        Guid tenantId, Guid warehouseId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                w.id, w.name, w.address_line1, w.city, w.state_province,
                w.postal_code, w.country, w.phone,
                w.is_primary, w.is_active, w.created_at, w.updated_at,
                COALESCE(sagg.slab_count,      0)   AS slab_count,
                COALESCE(sagg.available_count, 0)   AS available_count,
                COALESCE(sagg.reserved_count,  0)   AS reserved_count,
                COALESCE(sagg.on_hold_count,   0)   AS on_hold_count,
                sagg.estimated_value,
                COALESCE(pagg.product_sku_count, 0) AS product_sku_count,
                COALESCE(pagg.low_stock_count,   0) AS low_stock_count,
                pagg.product_stock_value
            FROM warehouses w
            LEFT JOIN (
                SELECT
                    s.warehouse_id,
                    COUNT(*)                                                               AS slab_count,
                    COUNT(*) FILTER (WHERE s.status = 'available')                        AS available_count,
                    COUNT(*) FILTER (WHERE s.status = 'reserved')                         AS reserved_count,
                    COUNT(*) FILTER (WHERE s.status = 'hold')                             AS on_hold_count,
                    ROUND(SUM(s.net_sqft * COALESCE(s.price_override, pv.base_price, 0))
                          FILTER (WHERE s.status = 'available'), 2)                       AS estimated_value
                FROM   slabs s
                JOIN   product_variants pv ON pv.id = s.variant_id
                WHERE  s.is_active = TRUE AND s.tenant_id = @tenantId
                GROUP BY s.warehouse_id
            ) sagg ON sagg.warehouse_id = w.id
            LEFT JOIN (
                SELECT
                    wps.warehouse_id,
                    COUNT(*)                                                                     AS product_sku_count,
                    COUNT(*) FILTER (
                        WHERE wps.reorder_point IS NOT NULL AND wps.qty_on_hand <= wps.reorder_point
                    )                                                                            AS low_stock_count,
                    ROUND(SUM(wps.qty_on_hand * pv2.base_price), 2)                             AS product_stock_value
                FROM   warehouse_product_stock wps
                JOIN   product_variants pv2 ON pv2.id = wps.variant_id
                WHERE  wps.tenant_id = @tenantId AND pv2.is_slab_variant = FALSE
                GROUP BY wps.warehouse_id
            ) pagg ON pagg.warehouse_id = w.id
            WHERE  w.tenant_id = @tenantId AND w.id = @warehouseId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<WarehouseRow>(sql, new { tenantId, warehouseId });
        return row is null ? null : MapToDto(row);
    }

    // ── Create ─────────────────────────────────────────────────────────────
    public async Task<WarehouseDto> CreateAsync(
        Guid tenantId, CreateWarehouseRequest req, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);

        // Demote current primary if needed
        if (req.SetAsPrimary)
        {
            await conn.ExecuteAsync(
                "UPDATE warehouses SET is_primary = FALSE WHERE tenant_id = @tenantId AND is_primary = TRUE",
                new { tenantId });
        }

        const string sql = """
            INSERT INTO warehouses (
                tenant_id, name, address_line1, city, state_province,
                postal_code, country, phone, is_primary, is_active
            ) VALUES (
                @tenantId, @name, @addressLine1, @city, @stateProvince,
                @postalCode, UPPER(COALESCE(@country, 'US')), @phone, @isPrimary, TRUE
            )
            RETURNING id, name, address_line1, city, state_province,
                      postal_code, country, phone, is_primary, is_active,
                      created_at, updated_at
            """;

        var row = await conn.QuerySingleAsync<WarehouseRow>(sql, new
        {
            tenantId,
            name          = req.Name,
            addressLine1  = req.AddressLine1,
            city          = req.City,
            stateProvince = req.StateProvince,
            postalCode    = req.PostalCode,
            country       = req.Country ?? "US",
            phone         = req.Phone,
            isPrimary     = req.SetAsPrimary,
        });

        return MapToDto(row);
    }

    // ── Update ─────────────────────────────────────────────────────────────
    public async Task<WarehouseDto> UpdateAsync(
        Guid tenantId, Guid warehouseId, UpdateWarehouseRequest req, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE warehouses SET
                name           = @name,
                address_line1  = @addressLine1,
                city           = @city,
                state_province = @stateProvince,
                postal_code    = @postalCode,
                country        = UPPER(COALESCE(@country, country)),
                phone          = @phone,
                updated_at     = NOW()
            WHERE tenant_id = @tenantId AND id = @warehouseId AND is_active = TRUE
            RETURNING id, name, address_line1, city, state_province,
                      postal_code, country, phone, is_primary, is_active,
                      created_at, updated_at
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<WarehouseRow>(sql, new
        {
            tenantId,
            warehouseId,
            name          = req.Name,
            addressLine1  = req.AddressLine1,
            city          = req.City,
            stateProvince = req.StateProvince,
            postalCode    = req.PostalCode,
            country       = req.Country,
            phone         = req.Phone,
        });

        return MapToDto(row!);
    }

    // ── Set primary ────────────────────────────────────────────────────────
    public async Task SetPrimaryAsync(Guid tenantId, Guid warehouseId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE warehouses SET is_primary = FALSE WHERE tenant_id = @tenantId AND is_primary = TRUE",
            new { tenantId });
        await conn.ExecuteAsync(
            "UPDATE warehouses SET is_primary = TRUE, updated_at = NOW() WHERE tenant_id = @tenantId AND id = @warehouseId",
            new { tenantId, warehouseId });
    }

    // ── Deactivate (soft delete) ───────────────────────────────────────────
    public async Task DeactivateAsync(Guid tenantId, Guid warehouseId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE warehouses
            SET is_active = FALSE, is_primary = FALSE, updated_at = NOW()
            WHERE tenant_id = @tenantId AND id = @warehouseId
            """;
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { tenantId, warehouseId });
    }

    // ── Transfer slabs between warehouses ──────────────────────────────────
    public async Task<int> TransferSlabsAsync(
        Guid tenantId, IEnumerable<Guid> slabIds, Guid targetWarehouseId,
        string? rackLocation, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE slabs
            SET warehouse_id  = @targetWarehouseId,
                rack_location = COALESCE(@rackLocation, rack_location),
                updated_at    = NOW()
            WHERE tenant_id = @tenantId
              AND id = ANY(@slabIds)
              AND is_active = TRUE
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.ExecuteAsync(sql, new
        {
            tenantId,
            targetWarehouseId,
            rackLocation,
            slabIds = slabIds.ToArray(),
        });
    }

    // ── Mapping ────────────────────────────────────────────────────────────
    private static WarehouseDto MapToDto(WarehouseRow r) => new()
    {
        Id                 = r.Id,
        Name               = r.Name,
        AddressLine1       = r.AddressLine1,
        City               = r.City,
        StateProvince      = r.StateProvince,
        PostalCode         = r.PostalCode,
        Country            = r.Country,
        Phone              = r.Phone,
        IsPrimary          = r.IsPrimary,
        IsActive           = r.IsActive,
        SlabCount          = r.SlabCount,
        AvailableCount     = r.AvailableCount,
        ReservedCount      = r.ReservedCount,
        OnHoldCount        = r.OnHoldCount,
        EstimatedValue     = r.EstimatedValue,
        ProductSkuCount    = r.ProductSkuCount,
        LowStockCount      = r.LowStockCount,
        ProductStockValue  = r.ProductStockValue,
        CreatedAt          = r.CreatedAt,
        UpdatedAt          = r.UpdatedAt,
    };

    private sealed class WarehouseRow
    {
        public Guid     Id                { get; init; }
        public string   Name              { get; init; } = string.Empty;
        public string?  AddressLine1      { get; init; }
        public string?  City              { get; init; }
        public string?  StateProvince     { get; init; }
        public string?  PostalCode        { get; init; }
        public string   Country           { get; init; } = "US";
        public string?  Phone             { get; init; }
        public bool     IsPrimary         { get; init; }
        public bool     IsActive          { get; init; }
        public int      SlabCount         { get; init; }
        public int      AvailableCount    { get; init; }
        public int      ReservedCount     { get; init; }
        public int      OnHoldCount       { get; init; }
        public decimal? EstimatedValue    { get; init; }
        public int      ProductSkuCount   { get; init; }
        public int      LowStockCount     { get; init; }
        public decimal? ProductStockValue { get; init; }
        public DateTime CreatedAt         { get; init; }
        public DateTime UpdatedAt         { get; init; }
    }
}
