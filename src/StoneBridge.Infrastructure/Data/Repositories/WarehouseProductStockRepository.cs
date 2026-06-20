using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class WarehouseProductStockRepository : IWarehouseProductStockRepository
{
    private readonly IDbConnectionFactory _db;

    public WarehouseProductStockRepository(IDbConnectionFactory db) => _db = db;

    // ── List product stock at a warehouse ─────────────────────────────────
    public async Task<(IReadOnlyList<WarehouseProductStockDto> Items, int TotalCount)> GetByWarehouseAsync(
        Guid tenantId, Guid warehouseId,
        WarehouseProductStockFilterParams filter,
        CancellationToken ct = default)
    {
        var where = new List<string>
        {
            "wps.tenant_id    = @tenantId",
            "wps.warehouse_id = @warehouseId",
            "pv.is_slab_variant = FALSE",
        };

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Add("(pv.sku ILIKE @SearchPat OR pv.variant_name ILIKE @SearchPat)");
        }

        if (!string.IsNullOrWhiteSpace(filter.CategoryCode))
        {
            where.Add("p.category_code = @CategoryCode");
        }

        if (filter.LowStockOnly == true)
        {
            where.Add("wps.reorder_point IS NOT NULL AND wps.qty_on_hand <= wps.reorder_point");
        }

        var predicate = "WHERE " + string.Join(" AND ", where);

        var countSql = $"""
            SELECT COUNT(*)
            FROM   warehouse_product_stock wps
            JOIN   product_variants pv ON pv.id = wps.variant_id
            JOIN   products p          ON p.id  = pv.product_id
            {predicate}
            """;

        var dataSql = $"""
            SELECT
                wps.id, wps.warehouse_id, wps.variant_id,
                p.id          AS product_id,
                pv.sku, pv.variant_name, pv.unit_of_measure, pv.base_price, pv.currency,
                pv.primary_photo_url,
                COALESCE(pc.code,  '')  AS category_code,
                COALESCE(pc.label, '')  AS category_label,
                wps.qty_on_hand, wps.qty_reserved,
                wps.rack_location, wps.reorder_point, wps.reorder_qty,
                wps.updated_at
            FROM   warehouse_product_stock wps
            JOIN   product_variants pv ON pv.id = wps.variant_id
            JOIN   products p          ON p.id  = pv.product_id
            LEFT JOIN product_categories pc ON pc.code = p.category_code
            {predicate}
            ORDER  BY wps.qty_on_hand <= COALESCE(wps.reorder_point, 999999) DESC,
                      pv.variant_name ASC
            LIMIT  @PerPage OFFSET @Offset
            """;

        var args = new
        {
            tenantId,
            warehouseId,
            SearchPat    = $"%{filter.Search}%",
            CategoryCode = filter.CategoryCode,
            PerPage      = filter.PerPage,
            Offset       = (filter.Page - 1) * filter.PerPage,
        };

        using var conn = await _db.CreateConnectionAsync(ct);
        var total = await conn.ExecuteScalarAsync<int>(countSql, args);
        var rows  = await conn.QueryAsync<StockRow>(dataSql, args);
        return (rows.Select(MapToDto).ToList(), total);
    }

    // ── KPI summary for a warehouse ───────────────────────────────────────
    public async Task<WarehouseProductStockSummary> GetSummaryAsync(
        Guid tenantId, Guid warehouseId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(*)                                                                AS total_skus,
                COUNT(*) FILTER (
                    WHERE wps.reorder_point IS NOT NULL
                      AND wps.qty_on_hand <= wps.reorder_point)                        AS low_stock_count,
                ROUND(SUM(wps.qty_on_hand * pv.base_price), 2)                        AS total_value
            FROM   warehouse_product_stock wps
            JOIN   product_variants pv ON pv.id = wps.variant_id
            WHERE  wps.tenant_id    = @tenantId
              AND  wps.warehouse_id = @warehouseId
              AND  pv.is_slab_variant = FALSE
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<SummaryRow>(sql, new { tenantId, warehouseId });
        return row is null
            ? new WarehouseProductStockSummary()
            : new WarehouseProductStockSummary
            {
                TotalSkus     = row.TotalSkus,
                LowStockCount = row.LowStockCount,
                TotalValue    = row.TotalValue,
            };
    }

    // ── Stock movement audit log ──────────────────────────────────────────
    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid tenantId, Guid warehouseId, int limit = 100,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                sm.id, sm.variant_id,
                pv.variant_name, pv.sku,
                sm.from_warehouse,
                wf.name AS from_warehouse_name,
                sm.to_warehouse,
                wt.name AS to_warehouse_name,
                sm.qty, sm.movement_type, sm.notes, sm.created_at
            FROM   stock_movements sm
            JOIN   product_variants pv ON pv.id = sm.variant_id
            LEFT JOIN warehouses wf ON wf.id = sm.from_warehouse
            LEFT JOIN warehouses wt ON wt.id = sm.to_warehouse
            WHERE  sm.tenant_id = @tenantId
              AND  (sm.from_warehouse = @warehouseId OR sm.to_warehouse = @warehouseId)
            ORDER  BY sm.created_at DESC
            LIMIT  @limit
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<MovementRow>(sql, new { tenantId, warehouseId, limit });
        return rows.Select(MapMovement).ToList();
    }

    // ── Receive stock (upsert + log) ──────────────────────────────────────
    public async Task<WarehouseProductStockDto> ReceiveStockAsync(
        Guid tenantId, Guid warehouseId,
        ReceiveStockRequest request,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);

        const string upsertSql = """
            INSERT INTO warehouse_product_stock
                (tenant_id, warehouse_id, variant_id, qty_on_hand, rack_location)
            VALUES
                (@tenantId, @warehouseId, @variantId, @qty, @rackLocation)
            ON CONFLICT (tenant_id, warehouse_id, variant_id) DO UPDATE
            SET qty_on_hand   = warehouse_product_stock.qty_on_hand + @qty,
                rack_location = COALESCE(@rackLocation, warehouse_product_stock.rack_location),
                updated_at    = NOW()
            RETURNING id
            """;

        await conn.ExecuteAsync(upsertSql, new
        {
            tenantId,
            warehouseId,
            variantId    = request.VariantId,
            qty          = request.Qty,
            rackLocation = request.RackLocation,
        });

        const string logSql = """
            INSERT INTO stock_movements (tenant_id, variant_id, to_warehouse, qty, movement_type, notes)
            VALUES (@tenantId, @variantId, @warehouseId, @qty, 'receive', @notes)
            """;

        await conn.ExecuteAsync(logSql, new
        {
            tenantId,
            variantId   = request.VariantId,
            warehouseId,
            qty         = request.Qty,
            notes       = request.Notes,
        });

        var updated = await GetSingleStockRowAsync(conn, tenantId, warehouseId, request.VariantId);
        return MapToDto(updated!);
    }

    // ── Transfer between warehouses ───────────────────────────────────────
    public async Task<(int FromQty, int ToQty)> TransferStockAsync(
        Guid tenantId, Guid fromWarehouseId,
        TransferWarehouseStockRequest request,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);

        // Deduct from source
        const string deductSql = """
            UPDATE warehouse_product_stock
            SET    qty_on_hand = qty_on_hand - @qty, updated_at = NOW()
            WHERE  tenant_id = @tenantId AND warehouse_id = @fromWarehouseId AND variant_id = @variantId
              AND  qty_on_hand - qty_reserved >= @qty
            RETURNING qty_on_hand
            """;

        var fromQty = await conn.ExecuteScalarAsync<int?>(deductSql, new
        {
            tenantId,
            fromWarehouseId,
            variantId = request.VariantId,
            qty       = request.Qty,
        });

        if (fromQty is null)
        {
            throw new InvalidOperationException("Insufficient available stock for transfer.");
        }

        // Add to destination
        const string upsertSql = """
            INSERT INTO warehouse_product_stock
                (tenant_id, warehouse_id, variant_id, qty_on_hand, rack_location)
            VALUES
                (@tenantId, @toWarehouseId, @variantId, @qty, @rackLocation)
            ON CONFLICT (tenant_id, warehouse_id, variant_id) DO UPDATE
            SET qty_on_hand   = warehouse_product_stock.qty_on_hand + @qty,
                rack_location = COALESCE(@rackLocation, warehouse_product_stock.rack_location),
                updated_at    = NOW()
            RETURNING qty_on_hand
            """;

        var toQty = await conn.ExecuteScalarAsync<int>(upsertSql, new
        {
            tenantId,
            toWarehouseId = request.ToWarehouseId,
            variantId     = request.VariantId,
            qty           = request.Qty,
            rackLocation  = request.ToRackLocation,
        });

        const string logSql = """
            INSERT INTO stock_movements
                (tenant_id, variant_id, from_warehouse, to_warehouse, qty, movement_type, notes)
            VALUES
                (@tenantId, @variantId, @fromWarehouseId, @toWarehouseId, @qty, 'transfer_in', @notes)
            """;

        await conn.ExecuteAsync(logSql, new
        {
            tenantId,
            variantId       = request.VariantId,
            fromWarehouseId,
            toWarehouseId   = request.ToWarehouseId,
            qty             = request.Qty,
            notes           = request.Notes,
        });

        return (fromQty.Value, toQty);
    }

    // ── Adjust stock (reconciliation) ─────────────────────────────────────
    public async Task<WarehouseProductStockDto> AdjustStockAsync(
        Guid tenantId, Guid warehouseId,
        AdjustWarehouseStockRequest request,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);

        const string getSql = """
            SELECT qty_on_hand FROM warehouse_product_stock
            WHERE tenant_id = @tenantId AND warehouse_id = @warehouseId AND variant_id = @variantId
            """;

        var current = await conn.ExecuteScalarAsync<int?>(getSql, new
        {
            tenantId, warehouseId, variantId = request.VariantId,
        });

        const string upsertSql = """
            INSERT INTO warehouse_product_stock
                (tenant_id, warehouse_id, variant_id, qty_on_hand)
            VALUES
                (@tenantId, @warehouseId, @variantId, @newQty)
            ON CONFLICT (tenant_id, warehouse_id, variant_id) DO UPDATE
            SET qty_on_hand = @newQty, updated_at = NOW()
            """;

        await conn.ExecuteAsync(upsertSql, new
        {
            tenantId,
            warehouseId,
            variantId = request.VariantId,
            newQty    = request.NewQtyOnHand,
        });

        var delta = request.NewQtyOnHand - (current ?? 0);
        var notes = $"[{request.Reason}] {request.Notes}".Trim();

        const string logSql = """
            INSERT INTO stock_movements
                (tenant_id, variant_id, to_warehouse, qty, movement_type, notes)
            VALUES
                (@tenantId, @variantId, @warehouseId, @qty, 'adjustment', @notes)
            """;

        await conn.ExecuteAsync(logSql, new
        {
            tenantId,
            variantId   = request.VariantId,
            warehouseId,
            qty         = delta,
            notes,
        });

        var updated = await GetSingleStockRowAsync(conn, tenantId, warehouseId, request.VariantId);
        return MapToDto(updated!);
    }

    // ── Set reorder point ─────────────────────────────────────────────────
    public async Task SetReorderPointAsync(
        Guid tenantId, Guid warehouseId,
        SetReorderPointRequest request,
        CancellationToken ct = default)
    {
        const string sql = """
            UPDATE warehouse_product_stock
            SET    reorder_point = @reorderPoint,
                   reorder_qty   = @reorderQty,
                   updated_at    = NOW()
            WHERE  tenant_id    = @tenantId
              AND  warehouse_id = @warehouseId
              AND  variant_id   = @variantId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            tenantId,
            warehouseId,
            variantId    = request.VariantId,
            reorderPoint = request.ReorderPoint,
            reorderQty   = request.ReorderQty,
        });
    }

    // ── Low stock across all (or one) warehouse ───────────────────────────
    public async Task<IReadOnlyList<WarehouseProductStockDto>> GetLowStockAsync(
        Guid tenantId, Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var whFilter = warehouseId.HasValue ? "AND wps.warehouse_id = @warehouseId" : "";

        var sql = $"""
            SELECT
                wps.id, wps.warehouse_id, wps.variant_id,
                p.id          AS product_id,
                pv.sku, pv.variant_name, pv.unit_of_measure, pv.base_price, pv.currency,
                pv.primary_photo_url,
                COALESCE(pc.code,  '') AS category_code,
                COALESCE(pc.label, '') AS category_label,
                wps.qty_on_hand, wps.qty_reserved,
                wps.rack_location, wps.reorder_point, wps.reorder_qty,
                wps.updated_at
            FROM   warehouse_product_stock wps
            JOIN   product_variants pv ON pv.id = wps.variant_id
            JOIN   products p          ON p.id  = pv.product_id
            LEFT JOIN product_categories pc ON pc.code = p.category_code
            WHERE  wps.tenant_id = @tenantId
              AND  pv.is_slab_variant = FALSE
              AND  wps.reorder_point IS NOT NULL
              AND  wps.qty_on_hand <= wps.reorder_point
              {whFilter}
            ORDER  BY (wps.qty_on_hand::float / NULLIF(wps.reorder_point, 0)) ASC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<StockRow>(sql, new { tenantId, warehouseId });
        return rows.Select(MapToDto).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static async Task<StockRow?> GetSingleStockRowAsync(
        System.Data.IDbConnection conn, Guid tenantId, Guid warehouseId, Guid variantId)
    {
        const string sql = """
            SELECT
                wps.id, wps.warehouse_id, wps.variant_id,
                p.id AS product_id,
                pv.sku, pv.variant_name, pv.unit_of_measure, pv.base_price, pv.currency,
                pv.primary_photo_url,
                COALESCE(pc.code,  '') AS category_code,
                COALESCE(pc.label, '') AS category_label,
                wps.qty_on_hand, wps.qty_reserved,
                wps.rack_location, wps.reorder_point, wps.reorder_qty,
                wps.updated_at
            FROM   warehouse_product_stock wps
            JOIN   product_variants pv ON pv.id = wps.variant_id
            JOIN   products p          ON p.id  = pv.product_id
            LEFT JOIN product_categories pc ON pc.code = p.category_code
            WHERE  wps.tenant_id    = @tenantId
              AND  wps.warehouse_id = @warehouseId
              AND  wps.variant_id   = @variantId
            """;

        return await conn.QuerySingleOrDefaultAsync<StockRow>(sql, new { tenantId, warehouseId, variantId });
    }

    private static WarehouseProductStockDto MapToDto(StockRow r) => new()
    {
        Id              = r.Id,
        WarehouseId     = r.WarehouseId,
        VariantId       = r.VariantId,
        ProductId       = r.ProductId,
        Sku             = r.Sku,
        VariantName     = r.VariantName,
        CategoryCode    = r.CategoryCode,
        CategoryLabel   = r.CategoryLabel,
        UnitOfMeasure   = r.UnitOfMeasure,
        BasePrice       = r.BasePrice,
        Currency        = r.Currency,
        QtyOnHand       = r.QtyOnHand,
        QtyReserved     = r.QtyReserved,
        RackLocation    = r.RackLocation,
        ReorderPoint    = r.ReorderPoint,
        ReorderQty      = r.ReorderQty,
        PrimaryPhotoUrl = r.PrimaryPhotoUrl,
        UpdatedAt       = r.UpdatedAt,
    };

    private static StockMovementDto MapMovement(MovementRow r) => new()
    {
        Id                = r.Id,
        VariantId         = r.VariantId,
        VariantName       = r.VariantName,
        Sku               = r.Sku,
        FromWarehouse     = r.FromWarehouse,
        FromWarehouseName = r.FromWarehouseName,
        ToWarehouse       = r.ToWarehouse,
        ToWarehouseName   = r.ToWarehouseName,
        Qty               = r.Qty,
        MovementType      = r.MovementType,
        Notes             = r.Notes,
        CreatedAt         = r.CreatedAt,
    };

    private sealed class StockRow
    {
        public Guid     Id              { get; init; }
        public Guid     WarehouseId     { get; init; }
        public Guid     VariantId       { get; init; }
        public Guid     ProductId       { get; init; }
        public string   Sku             { get; init; } = string.Empty;
        public string   VariantName     { get; init; } = string.Empty;
        public string   CategoryCode    { get; init; } = string.Empty;
        public string   CategoryLabel   { get; init; } = string.Empty;
        public string   UnitOfMeasure   { get; init; } = "each";
        public decimal  BasePrice       { get; init; }
        public string   Currency        { get; init; } = "USD";
        public string?  PrimaryPhotoUrl { get; init; }
        public int      QtyOnHand       { get; init; }
        public int      QtyReserved     { get; init; }
        public string?  RackLocation    { get; init; }
        public int?     ReorderPoint    { get; init; }
        public int?     ReorderQty      { get; init; }
        public DateTime UpdatedAt       { get; init; }
    }

    private sealed class SummaryRow
    {
        public int      TotalSkus     { get; init; }
        public int      LowStockCount { get; init; }
        public decimal? TotalValue    { get; init; }
    }

    private sealed class MovementRow
    {
        public Guid     Id                { get; init; }
        public Guid     VariantId         { get; init; }
        public string   VariantName       { get; init; } = string.Empty;
        public string   Sku               { get; init; } = string.Empty;
        public Guid?    FromWarehouse     { get; init; }
        public string?  FromWarehouseName { get; init; }
        public Guid?    ToWarehouse       { get; init; }
        public string?  ToWarehouseName   { get; init; }
        public int      Qty               { get; init; }
        public string   MovementType      { get; init; } = string.Empty;
        public string?  Notes             { get; init; }
        public DateTime CreatedAt         { get; init; }
    }
}
