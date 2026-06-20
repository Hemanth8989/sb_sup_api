using System.Text;
using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly IDbConnectionFactory _db;

    public WarehouseRepository(IDbConnectionFactory db) => _db = db;

    // shared subquery fragments
    private const string SlabAgg = """
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
        """;

    private const string ProductAgg = """
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
        """;

    private const string SelectCols = """
        SELECT
            w.id, w.name, w.address_line1, w.city, w.state_province,
            w.postal_code, w.country, w.phone, w.capacity_sqft, w.notes,
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
        """;

    // ── List ──────────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var sql = $"""
            {SelectCols}
            {SlabAgg}
            {ProductAgg}
            WHERE  w.tenant_id = @tenantId AND w.is_active = TRUE
            ORDER BY w.is_primary DESC, w.name ASC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<WarehouseRow>(sql, new { tenantId });
        return rows.Select(MapToDto).ToList();
    }

    // ── Single ────────────────────────────────────────────────────────────────
    public async Task<WarehouseDto?> GetByIdAsync(
        Guid tenantId, Guid warehouseId, CancellationToken ct = default)
    {
        var sql = $"""
            {SelectCols}
            {SlabAgg}
            {ProductAgg}
            WHERE  w.tenant_id = @tenantId AND w.id = @warehouseId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<WarehouseRow>(sql, new { tenantId, warehouseId });
        return row is null ? null : MapToDto(row);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<WarehouseDto> CreateAsync(
        Guid tenantId, CreateWarehouseRequest req, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);

        if (req.SetAsPrimary)
        {
            await conn.ExecuteAsync(
                "UPDATE warehouses SET is_primary = FALSE WHERE tenant_id = @tenantId AND is_primary = TRUE",
                new { tenantId });
        }

        const string sql = """
            INSERT INTO warehouses (
                tenant_id, name, address_line1, city, state_province,
                postal_code, country, phone, is_primary, is_active,
                capacity_sqft, notes
            ) VALUES (
                @tenantId, @name, @addressLine1, @city, @stateProvince,
                @postalCode, UPPER(COALESCE(@country, 'US')), @phone, @isPrimary, TRUE,
                @capacitySqft, @notes
            )
            RETURNING id, name, address_line1, city, state_province,
                      postal_code, country, phone, is_primary, is_active,
                      capacity_sqft, notes, created_at, updated_at
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
            capacitySqft  = req.CapacitySqft,
            notes         = req.Notes,
        });

        return MapToDto(row);
    }

    // ── Update ────────────────────────────────────────────────────────────────
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
                capacity_sqft  = @capacitySqft,
                notes          = @notes,
                updated_at     = NOW()
            WHERE tenant_id = @tenantId AND id = @warehouseId AND is_active = TRUE
            RETURNING id, name, address_line1, city, state_province,
                      postal_code, country, phone, is_primary, is_active,
                      capacity_sqft, notes, created_at, updated_at
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
            capacitySqft  = req.CapacitySqft,
            notes         = req.Notes,
        });

        return MapToDto(row!);
    }

    // ── Set primary ───────────────────────────────────────────────────────────
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

    // ── Deactivate ────────────────────────────────────────────────────────────
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

    // ── Transfer slabs (with audit log) ──────────────────────────────────────
    public async Task<int> TransferSlabsAsync(
        Guid tenantId, IEnumerable<Guid> slabIds, Guid targetWarehouseId,
        string? rackLocation, CancellationToken ct = default)
    {
        var slabIdArr = slabIds.ToArray();
        if (slabIdArr.Length == 0) { return 0; }

        const string fetchSql = """
            SELECT id, warehouse_id AS from_warehouse, rack_location AS from_rack
            FROM   slabs
            WHERE  tenant_id = @tenantId AND id = ANY(@slabIds) AND is_active = TRUE
            """;

        const string updateSql = """
            UPDATE slabs
            SET warehouse_id  = @targetWarehouseId,
                rack_location = COALESCE(@rackLocation, rack_location),
                updated_at    = NOW()
            WHERE tenant_id = @tenantId
              AND id = ANY(@slabIds)
              AND is_active = TRUE
            """;

        const string logSql = """
            INSERT INTO slab_transfer_log
                (tenant_id, slab_id, from_warehouse, to_warehouse, from_rack, to_rack)
            SELECT @tenantId, id, from_warehouse, @targetWarehouseId, from_rack, @toRack
            FROM   UNNEST(@slabIds::uuid[], @fromWarehouses::uuid[], @fromRacks::text[])
                   AS t(id, from_warehouse, from_rack)
            """;

        using var conn = await _db.CreateConnectionAsync(ct);

        // Fetch current warehouse assignments before updating
        var current = (await conn.QueryAsync<SlabLocationRow>(fetchSql, new { tenantId, slabIds = slabIdArr })).ToList();

        using var txn = conn.BeginTransaction();

        var updated = await conn.ExecuteAsync(updateSql, new
        {
            tenantId,
            targetWarehouseId,
            rackLocation,
            slabIds = slabIdArr,
        }, txn);

        if (updated > 0 && current.Count > 0)
        {
            await conn.ExecuteAsync(logSql, new
            {
                tenantId,
                targetWarehouseId,
                toRack        = rackLocation,
                slabIds       = current.Select(r => r.Id).ToArray(),
                fromWarehouses = current.Select(r => r.FromWarehouse).ToArray(),
                fromRacks     = current.Select(r => r.FromRack).ToArray(),
            }, txn);
        }

        txn.Commit();
        return updated;
    }

    // ── Audit log ─────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<WarehouseAuditEventDto>> GetAuditLogAsync(
        Guid tenantId, Guid warehouseId, int limit = 200, CancellationToken ct = default)
    {
        const string sql = """
            SELECT * FROM (

                -- Product stock movements
                SELECT
                    sm.id::text                                         AS id,
                    'stock_movement'                                    AS event_source,
                    sm.movement_type                                    AS event_type,
                    CASE sm.movement_type
                        WHEN 'receive'      THEN 'Received '    || sm.qty || ' ' || pv.unit_of_measure || ' of ' || pv.sku
                        WHEN 'transfer_out' THEN 'Transferred ' || sm.qty || ' ' || pv.unit_of_measure || ' of ' || pv.sku
                                                 || ' → ' || COALESCE(tw.name, 'another warehouse')
                        WHEN 'transfer_in'  THEN 'Received '    || sm.qty || ' ' || pv.unit_of_measure || ' of ' || pv.sku
                                                 || ' from ' || COALESCE(fw.name, 'another warehouse')
                        ELSE                     'Stock adjusted: ' || sm.qty || ' ' || pv.unit_of_measure || ' of ' || pv.sku
                    END                                                 AS description,
                    pv.sku                                              AS subject_ref,
                    NULL::text                                          AS old_value,
                    sm.notes                                            AS new_value,
                    sm.notes,
                    sm.qty::numeric                                     AS qty,
                    sm.created_at
                FROM  stock_movements sm
                JOIN  product_variants pv ON pv.id = sm.variant_id
                LEFT JOIN warehouses fw ON fw.id = sm.from_warehouse
                LEFT JOIN warehouses tw ON tw.id = sm.to_warehouse
                WHERE sm.tenant_id = @tenantId
                  AND (sm.from_warehouse = @warehouseId OR sm.to_warehouse = @warehouseId)

                UNION ALL

                -- Slab transfers
                SELECT
                    stl.id::text,
                    'slab_transfer',
                    'slab_transfer',
                    'Slab ' || s.internal_ref || ' moved from '
                        || COALESCE(fw2.name, 'external') || ' → ' || tw2.name,
                    s.internal_ref,
                    fw2.name,
                    tw2.name,
                    stl.notes,
                    NULL::numeric,
                    stl.transferred_at
                FROM  slab_transfer_log stl
                JOIN  slabs s    ON s.id  = stl.slab_id
                JOIN  warehouses tw2 ON tw2.id = stl.to_warehouse
                LEFT JOIN warehouses fw2 ON fw2.id = stl.from_warehouse
                WHERE stl.tenant_id = @tenantId
                  AND (stl.from_warehouse = @warehouseId OR stl.to_warehouse = @warehouseId)

                UNION ALL

                -- Slab status / price events (slabs currently in this warehouse)
                SELECT
                    se.id::text,
                    'slab_event',
                    se.event_type,
                    'Slab ' || s2.internal_ref || ' ' || REPLACE(se.event_type, '_', ' ')
                        || CASE WHEN se.new_value IS NOT NULL THEN ' → ' || se.new_value ELSE '' END,
                    s2.internal_ref,
                    se.old_value,
                    se.new_value,
                    se.notes,
                    NULL::numeric,
                    se.created_at
                FROM  slab_events se
                JOIN  slabs s2 ON s2.id = se.slab_id AND s2.warehouse_id = @warehouseId
                WHERE se.tenant_id = @tenantId

            ) events
            ORDER BY created_at DESC
            LIMIT @limit
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<AuditRow>(sql, new { tenantId, warehouseId, limit });
        return rows.Select(r => new WarehouseAuditEventDto
        {
            Id          = r.Id,
            EventSource = r.EventSource,
            EventType   = r.EventType,
            Description = r.Description,
            SubjectRef  = r.SubjectRef,
            OldValue    = r.OldValue,
            NewValue    = r.NewValue,
            Notes       = r.Notes,
            Qty         = r.Qty,
            CreatedAt   = r.CreatedAt,
        }).ToList();
    }

    // ── Bundle / Lot grouped view ─────────────────────────────────────────────
    public async Task<IReadOnlyList<WarehouseBundleDto>> GetBundlesAsync(
        Guid tenantId, Guid warehouseId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                s.bundle_id::text                                                     AS bundle_id,
                CASE
                    WHEN s.bundle_id IS NOT NULL THEN b.bundle_ref
                    WHEN s.lot_number IS NOT NULL THEN s.lot_number
                    ELSE NULL
                END                                                                   AS group_ref,
                CASE
                    WHEN s.bundle_id IS NOT NULL THEN 'bundle'
                    WHEN s.lot_number IS NOT NULL THEN 'lot'
                    ELSE 'ungrouped'
                END                                                                   AS group_type,
                MIN(s.material_name)                                                  AS material_name,
                MIN(COALESCE(b.origin_country, s.origin_country))                     AS origin_country,
                MIN(COALESCE(b.quarry_name,    s.quarry_name))                        AS quarry_name,
                MIN(b.arrival_date)::text                                             AS arrival_date,
                MIN(b.invoice_ref)                                                    AS invoice_ref,
                COUNT(*)                                                              AS slab_count,
                COUNT(*) FILTER (WHERE s.status = 'available')                        AS available_count,
                COUNT(*) FILTER (WHERE s.status = 'reserved')                         AS reserved_count,
                COUNT(*) FILTER (WHERE s.status = 'hold')                             AS on_hold_count,
                ROUND(SUM(s.net_sqft), 2)                                             AS total_sqft,
                ROUND(SUM(s.net_sqft * COALESCE(s.price_override, pv.base_price, 0))
                      FILTER (WHERE s.status = 'available'), 2)                       AS estimated_value,
                MIN(s.created_at)                                                     AS first_received_at
            FROM  slabs s
            JOIN  product_variants pv ON pv.id = s.variant_id
            LEFT JOIN slab_bundles b  ON b.id  = s.bundle_id
            WHERE s.tenant_id    = @tenantId
              AND s.warehouse_id = @warehouseId
              AND s.is_active    = TRUE
            GROUP BY
                s.bundle_id,
                CASE WHEN s.bundle_id IS NULL THEN s.lot_number ELSE NULL END
            ORDER BY
                CASE
                    WHEN s.bundle_id IS NOT NULL THEN 0
                    WHEN s.lot_number IS NOT NULL THEN 1
                    ELSE 2
                END,
                slab_count DESC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<BundleRow>(sql, new { tenantId, warehouseId });
        return rows.Select(r => new WarehouseBundleDto
        {
            BundleId        = r.BundleId,
            GroupRef        = r.GroupRef,
            GroupType       = r.GroupType,
            MaterialName    = r.MaterialName,
            OriginCountry   = r.OriginCountry,
            QuarryName      = r.QuarryName,
            ArrivalDate     = r.ArrivalDate,
            InvoiceRef      = r.InvoiceRef,
            SlabCount       = r.SlabCount,
            AvailableCount  = r.AvailableCount,
            ReservedCount   = r.ReservedCount,
            OnHoldCount     = r.OnHoldCount,
            TotalSqft       = r.TotalSqft,
            EstimatedValue  = r.EstimatedValue,
            FirstReceivedAt = r.FirstReceivedAt,
        }).ToList();
    }

    // ── CSV export ────────────────────────────────────────────────────────────
    public async Task<string> ExportSlabsAsCsvAsync(
        Guid tenantId, Guid warehouseId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                s.internal_ref, s.material_type, s.material_name, s.color_family, s.pattern,
                s.origin_country, s.quarry_name, s.lot_number, s.block_number,
                s.thickness_cm, s.finish,
                s.gross_length_mm, s.gross_width_mm, s.net_sqft, s.net_sqm, s.weight_kg,
                s.quality_grade, s.status, s.is_remnant,
                s.price_override, pv.base_price, pv.currency,
                COALESCE(s.price_override, pv.base_price)  AS effective_price,
                s.rack_location, s.barcode, s.notes, s.created_at
            FROM  slabs s
            JOIN  product_variants pv ON pv.id = s.variant_id
            WHERE s.tenant_id    = @tenantId
              AND s.warehouse_id = @warehouseId
              AND s.is_active    = TRUE
            ORDER BY s.internal_ref ASC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<SlabCsvRow>(sql, new { tenantId, warehouseId });

        var sb = new StringBuilder();
        sb.AppendLine("InternalRef,MaterialType,MaterialName,ColorFamily,Pattern,Origin,Quarry," +
                      "LotNumber,BlockNumber,ThicknessCm,Finish,GrossLengthMm,GrossWidthMm," +
                      "NetSqft,NetSqm,WeightKg,QualityGrade,Status,IsRemnant," +
                      "PriceOverride,BasePrice,EffectivePrice,Currency,RackLocation,Barcode,Notes,CreatedAt");

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                CsvEsc(r.InternalRef),   CsvEsc(r.MaterialType),  CsvEsc(r.MaterialName),
                CsvEsc(r.ColorFamily),   CsvEsc(r.Pattern),        CsvEsc(r.OriginCountry),
                CsvEsc(r.QuarryName),    CsvEsc(r.LotNumber),      CsvEsc(r.BlockNumber),
                r.ThicknessCm?.ToString()       ?? "",
                CsvEsc(r.Finish),
                r.GrossLengthMm?.ToString()     ?? "",
                r.GrossWidthMm?.ToString()      ?? "",
                r.NetSqft?.ToString("F2")       ?? "",
                r.NetSqm?.ToString("F2")        ?? "",
                r.WeightKg?.ToString("F2")      ?? "",
                CsvEsc(r.QualityGrade),  CsvEsc(r.Status),
                r.IsRemnant.ToString().ToLowerInvariant(),
                r.PriceOverride?.ToString("F2") ?? "",
                r.BasePrice?.ToString("F2")     ?? "",
                r.EffectivePrice?.ToString("F2") ?? "",
                CsvEsc(r.Currency),      CsvEsc(r.RackLocation),   CsvEsc(r.Barcode),
                CsvEsc(r.Notes),         r.CreatedAt.ToString("yyyy-MM-dd")
            ));
        }

        return sb.ToString();
    }

    private static string CsvEsc(string? v)
    {
        if (v is null) { return ""; }
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
        {
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
        return v;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────
    private static WarehouseDto MapToDto(WarehouseRow r) => new()
    {
        Id                = r.Id,
        Name              = r.Name,
        AddressLine1      = r.AddressLine1,
        City              = r.City,
        StateProvince     = r.StateProvince,
        PostalCode        = r.PostalCode,
        Country           = r.Country,
        Phone             = r.Phone,
        IsPrimary         = r.IsPrimary,
        IsActive          = r.IsActive,
        SlabCount         = r.SlabCount,
        AvailableCount    = r.AvailableCount,
        ReservedCount     = r.ReservedCount,
        OnHoldCount       = r.OnHoldCount,
        EstimatedValue    = r.EstimatedValue,
        ProductSkuCount   = r.ProductSkuCount,
        LowStockCount     = r.LowStockCount,
        ProductStockValue = r.ProductStockValue,
        CapacitySqft      = r.CapacitySqft,
        Notes             = r.Notes,
        CreatedAt         = r.CreatedAt,
        UpdatedAt         = r.UpdatedAt,
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
        public decimal? CapacitySqft      { get; init; }
        public string?  Notes             { get; init; }
        public DateTime CreatedAt         { get; init; }
        public DateTime UpdatedAt         { get; init; }
    }

    private sealed class SlabLocationRow
    {
        public Guid  Id            { get; init; }
        public Guid? FromWarehouse { get; init; }
        public string? FromRack    { get; init; }
    }

    private sealed class AuditRow
    {
        public string   Id          { get; init; } = string.Empty;
        public string   EventSource { get; init; } = string.Empty;
        public string   EventType   { get; init; } = string.Empty;
        public string   Description { get; init; } = string.Empty;
        public string?  SubjectRef  { get; init; }
        public string?  OldValue    { get; init; }
        public string?  NewValue    { get; init; }
        public string?  Notes       { get; init; }
        public decimal? Qty         { get; init; }
        public DateTime CreatedAt   { get; init; }
    }

    private sealed class BundleRow
    {
        public string?  BundleId        { get; init; }
        public string?  GroupRef        { get; init; }
        public string   GroupType       { get; init; } = string.Empty;
        public string   MaterialName    { get; init; } = string.Empty;
        public string?  OriginCountry   { get; init; }
        public string?  QuarryName      { get; init; }
        public string?  ArrivalDate     { get; init; }
        public string?  InvoiceRef      { get; init; }
        public int      SlabCount       { get; init; }
        public int      AvailableCount  { get; init; }
        public int      ReservedCount   { get; init; }
        public int      OnHoldCount     { get; init; }
        public decimal? TotalSqft       { get; init; }
        public decimal? EstimatedValue  { get; init; }
        public DateTime FirstReceivedAt { get; init; }
    }

    private sealed class SlabCsvRow
    {
        public string   InternalRef    { get; init; } = string.Empty;
        public string   MaterialType   { get; init; } = string.Empty;
        public string   MaterialName   { get; init; } = string.Empty;
        public string?  ColorFamily    { get; init; }
        public string?  Pattern        { get; init; }
        public string?  OriginCountry  { get; init; }
        public string?  QuarryName     { get; init; }
        public string?  LotNumber      { get; init; }
        public string?  BlockNumber    { get; init; }
        public decimal? ThicknessCm    { get; init; }
        public string?  Finish         { get; init; }
        public int?     GrossLengthMm  { get; init; }
        public int?     GrossWidthMm   { get; init; }
        public decimal? NetSqft        { get; init; }
        public decimal? NetSqm         { get; init; }
        public decimal? WeightKg       { get; init; }
        public string?  QualityGrade   { get; init; }
        public string   Status         { get; init; } = string.Empty;
        public bool     IsRemnant      { get; init; }
        public decimal? PriceOverride  { get; init; }
        public decimal? BasePrice      { get; init; }
        public decimal? EffectivePrice { get; init; }
        public string?  Currency       { get; init; }
        public string?  RackLocation   { get; init; }
        public string?  Barcode        { get; init; }
        public string?  Notes          { get; init; }
        public DateTime CreatedAt      { get; init; }
    }
}
