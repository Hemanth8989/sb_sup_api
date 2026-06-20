using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

/// <summary>
/// Dapper-based implementation of ISupplierSlabRepository.
/// Returns all slabs owned by the supplier tenant (all statuses).
/// Unlike CatalogRepository, there is no connection JOIN — suppliers see their own stock.
/// Dynamic WHERE clause is built safely: only parameter names are interpolated, never user values.
/// </summary>
public sealed class SupplierSlabRepository : ISupplierSlabRepository
{
    private readonly IDbConnectionFactory           _connectionFactory;
    private readonly ILogger<SupplierSlabRepository> _logger;

    public SupplierSlabRepository(
        IDbConnectionFactory            connectionFactory,
        ILogger<SupplierSlabRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger            = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SupplierSlabDto>> GetInventoryAsync(
        Guid                    supplierId,
        SupplierSlabFilterParams filterParams,
        CancellationToken        ct = default)
    {
        var (whereClause, parameters) = BuildWhereClause(supplierId, filterParams);
        var orderClause               = BuildOrderClause(filterParams.SortBy, filterParams.SortDir);
        var offset                    = (filterParams.Page - 1) * filterParams.PerPage;

        parameters.Add("PerPage", filterParams.PerPage);
        parameters.Add("Offset",  offset);

        var countSql = $"""
            SELECT COUNT(*)
            FROM   slabs s
            JOIN   product_variants v  ON v.id = s.variant_id
            LEFT JOIN warehouses w     ON w.id = s.warehouse_id
            LEFT JOIN slab_bundles b   ON b.id = s.bundle_id
            {whereClause}
            """;

        var dataSql = $"""
            SELECT
                s.id,
                s.variant_id,
                s.bundle_id,
                s.internal_ref,
                s.barcode,
                s.material_type,
                s.material_name,
                s.color_family,
                s.pattern,
                s.origin_country,
                s.quarry_name,
                s.lot_number,
                s.block_number,
                s.thickness_cm,
                s.finish,
                s.gross_length_mm,
                s.gross_width_mm,
                s.net_sqft,
                s.net_sqm,
                s.weight_kg,
                s.quality_grade,
                s.is_remnant,
                s.status,
                s.status_changed,
                s.reserved_for_po    AS reserved_for_po_id,
                s.price_override,
                s.rack_location,
                s.notes,
                s.warehouse_id,
                s.created_at,
                s.updated_at,
                v.base_price,
                v.currency,
                w.name               AS warehouse_name,
                w.city               AS warehouse_city,
                w.state_province     AS warehouse_state,
                b.bundle_ref,
                (
                    SELECT ph.url
                    FROM   slab_photos ph
                    WHERE  ph.slab_id    = s.id
                    ORDER  BY ph.sort_order ASC
                    LIMIT  1
                )                    AS primary_photo_url,
                (
                    SELECT ph.thumb_url
                    FROM   slab_photos ph
                    WHERE  ph.slab_id    = s.id
                    ORDER  BY ph.sort_order ASC
                    LIMIT  1
                )                    AS primary_thumb_url,
                (
                    SELECT COUNT(*)
                    FROM   slab_photos ph
                    WHERE  ph.slab_id = s.id
                )                    AS photo_count
            FROM   slabs s
            JOIN   product_variants v  ON v.id = s.variant_id
            LEFT JOIN warehouses w     ON w.id = s.warehouse_id
            LEFT JOIN slab_bundles b   ON b.id = s.bundle_id
            {whereClause}
            {orderClause}
            LIMIT  @PerPage OFFSET @Offset
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (totalCount == 0)
        {
            _logger.LogDebug(
                "Supplier inventory returned 0 results for supplier {SupplierId}", supplierId);

            return PagedResult<SupplierSlabDto>.Empty(filterParams.Page, filterParams.PerPage);
        }

        var rows = await connection.QueryAsync<SupplierSlabRow>(dataSql, parameters);

        return PagedResult<SupplierSlabDto>.Create(
            rows.Select(MapToDto),
            totalCount,
            filterParams.Page,
            filterParams.PerPage);
    }

    // ── WHERE clause builder ───────────────────────────────────────────────────
    private static (string whereClause, DynamicParameters parameters) BuildWhereClause(
        Guid                    supplierId,
        SupplierSlabFilterParams f)
    {
        var sb = new StringBuilder();
        var p  = new DynamicParameters();

        // Always filter to the authenticated supplier's own slabs + active records only
        sb.Append("""
            WHERE s.tenant_id = @SupplierId
              AND s.is_active  = TRUE
            """);

        p.Add("SupplierId", supplierId);

        // Status filter — multi-value, defaults to all statuses when empty
        if (f.Statuses.Count > 0)
        {
            var statuses = f.Statuses
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            if (statuses.Length > 0)
            {
                sb.Append("\n  AND s.status = ANY(@Statuses)");
                p.Add("Statuses", statuses);
            }
        }

        // Material types — multi-value
        if (f.MaterialTypes.Count > 0)
        {
            var types = f.MaterialTypes
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            if (types.Length > 0)
            {
                sb.Append("\n  AND s.material_type = ANY(@MaterialTypes)");
                p.Add("MaterialTypes", types);
            }
        }

        // Colour families — multi-value
        if (f.ColorFamilies.Count > 0)
        {
            var colours = f.ColorFamilies
                .Select(c => c.Trim().ToLowerInvariant())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToArray();

            if (colours.Length > 0)
            {
                sb.Append("\n  AND s.color_family = ANY(@ColorFamilies)");
                p.Add("ColorFamilies", colours);
            }
        }

        // Finishes — multi-value
        if (f.Finishes.Count > 0)
        {
            var finishes = f.Finishes
                .Select(fi => fi.Trim().ToLowerInvariant())
                .Where(fi => !string.IsNullOrEmpty(fi))
                .ToArray();

            if (finishes.Length > 0)
            {
                sb.Append("\n  AND s.finish = ANY(@Finishes)");
                p.Add("Finishes", finishes);
            }
        }

        // Thickness range
        if (f.ThicknessMinCm.HasValue)
        {
            sb.Append("\n  AND s.thickness_cm >= @ThicknessMinCm");
            p.Add("ThicknessMinCm", f.ThicknessMinCm.Value);
        }
        if (f.ThicknessMaxCm.HasValue)
        {
            sb.Append("\n  AND s.thickness_cm <= @ThicknessMaxCm");
            p.Add("ThicknessMaxCm", f.ThicknessMaxCm.Value);
        }

        // Minimum net sqft
        if (f.MinNetSqft.HasValue)
        {
            sb.Append("\n  AND s.net_sqft >= @MinNetSqft");
            p.Add("MinNetSqft", f.MinNetSqft.Value);
        }

        // Remnant flag
        if (f.IsRemnant.HasValue)
        {
            sb.Append("\n  AND s.is_remnant = @IsRemnant");
            p.Add("IsRemnant", f.IsRemnant.Value);
        }

        // Warehouse filter
        if (f.WarehouseId.HasValue)
        {
            sb.Append("\n  AND s.warehouse_id = @WarehouseId");
            p.Add("WarehouseId", f.WarehouseId.Value);
        }

        // Full-text search
        if (!string.IsNullOrWhiteSpace(f.SearchQuery))
        {
            var term = f.SearchQuery.Trim();

            if (term.Length >= 3)
            {
                sb.Append("\n  AND s.search_vector @@ plainto_tsquery('english', @SearchQuery)");
                p.Add("SearchQuery", term);
            }
            else
            {
                sb.Append("\n  AND (s.material_name ILIKE @SearchLike OR s.internal_ref ILIKE @SearchLike)");
                p.Add("SearchLike", $"%{term}%");
            }
        }

        return (sb.ToString(), p);
    }

    // ── ORDER BY builder ───────────────────────────────────────────────────────
    private static string BuildOrderClause(string sortBy, string sortDir)
    {
        var dir = string.Equals(sortDir, "ASC", StringComparison.OrdinalIgnoreCase)
            ? "ASC" : "DESC";

        return sortBy.ToLowerInvariant() switch
        {
            "status"                            => $"ORDER BY s.status {dir}, s.updated_at DESC",
            "internalref"    or "internal_ref"  => $"ORDER BY s.internal_ref {dir}",
            "netsqft"        or "net_sqft"      => $"ORDER BY s.net_sqft {dir}",
            "createdat"      or "created_at"    => $"ORDER BY s.created_at {dir}",
            "materialname"   or "material_name" => $"ORDER BY s.material_name {dir}",
            "effectiveprice" or "effective_price" => $"ORDER BY COALESCE(s.price_override, v.base_price) {dir}",
            _                                   => $"ORDER BY s.updated_at {dir}",
        };
    }

    // ── Dapper row mapping ─────────────────────────────────────────────────────
    // ── New CRUD methods ───────────────────────────────────────────────────────

    public async Task<SupplierSlabDto?> GetByIdAsync(
        Guid supplierId, Guid slabId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                s.id, s.variant_id, s.bundle_id, s.internal_ref, s.barcode,
                s.material_type, s.material_name, s.color_family, s.pattern,
                s.origin_country, s.quarry_name, s.lot_number, s.block_number,
                s.thickness_cm, s.finish, s.gross_length_mm, s.gross_width_mm,
                s.net_sqft, s.net_sqm, s.weight_kg, s.quality_grade, s.is_remnant,
                s.status, s.status_changed, s.reserved_for_po AS reserved_for_po_id,
                s.price_override, s.rack_location, s.notes, s.warehouse_id,
                s.created_at, s.updated_at,
                v.base_price, v.currency,
                w.name AS warehouse_name, w.city AS warehouse_city, w.state_province AS warehouse_state,
                b.bundle_ref,
                (SELECT ph.url       FROM slab_photos ph WHERE ph.slab_id = s.id ORDER BY ph.sort_order LIMIT 1) AS primary_photo_url,
                (SELECT ph.thumb_url FROM slab_photos ph WHERE ph.slab_id = s.id ORDER BY ph.sort_order LIMIT 1) AS primary_thumb_url,
                (SELECT COUNT(*)     FROM slab_photos ph WHERE ph.slab_id = s.id)                                 AS photo_count
            FROM slabs s
            JOIN product_variants v ON v.id = s.variant_id
            LEFT JOIN warehouses w  ON w.id = s.warehouse_id
            LEFT JOIN slab_bundles b ON b.id = s.bundle_id
            WHERE s.id = @SlabId AND s.tenant_id = @SupplierId AND s.is_active = TRUE
            """;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<SupplierSlabRow>(sql, new { SlabId = slabId, SupplierId = supplierId });
        return row is null ? null : MapToDto(row);
    }

    public async Task<SupplierSlabDto> CreateAsync(
        Guid supplierId, CreateSlabRequest req, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO slabs (
                tenant_id, variant_id, bundle_id, internal_ref, barcode,
                material_type, material_name, color_family, pattern,
                origin_country, quarry_name, lot_number, block_number,
                thickness_cm, finish, gross_length_mm, gross_width_mm,
                weight_kg, quality_grade, is_remnant,
                warehouse_id, rack_location, price_override, notes, status
            ) VALUES (
                @SupplierId, @VariantId, @BundleId, @InternalRef, @Barcode,
                @MaterialType, @MaterialName, @ColorFamily, @Pattern,
                @OriginCountry, @QuarryName, @LotNumber, @BlockNumber,
                @ThicknessCm, @Finish, @GrossLengthMm, @GrossWidthMm,
                @WeightKg, @QualityGrade, @IsRemnant,
                @WarehouseId, @RackLocation, @PriceOverride, @Notes, 'available'
            )
            RETURNING id
            """;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var newId = await conn.ExecuteScalarAsync<Guid>(sql, new
        {
            SupplierId    = supplierId,
            req.VariantId,
            req.BundleId,
            req.InternalRef,
            req.Barcode,
            req.MaterialType,
            req.MaterialName,
            req.ColorFamily,
            req.Pattern,
            req.OriginCountry,
            req.QuarryName,
            req.LotNumber,
            req.BlockNumber,
            req.ThicknessCm,
            req.Finish,
            req.GrossLengthMm,
            req.GrossWidthMm,
            req.WeightKg,
            req.QualityGrade,
            req.IsRemnant,
            req.WarehouseId,
            req.RackLocation,
            req.PriceOverride,
            req.Notes,
        });

        return (await GetByIdAsync(supplierId, newId, ct))!;
    }

    public async Task<SupplierSlabDto?> UpdateAsync(
        Guid supplierId, Guid slabId, UpdateSlabRequest req, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE slabs SET
                internal_ref   = @InternalRef,
                barcode        = @Barcode,
                material_type  = @MaterialType,
                material_name  = @MaterialName,
                color_family   = @ColorFamily,
                pattern        = @Pattern,
                origin_country = @OriginCountry,
                quarry_name    = @QuarryName,
                lot_number     = @LotNumber,
                block_number   = @BlockNumber,
                thickness_cm   = @ThicknessCm,
                finish         = @Finish,
                gross_length_mm = @GrossLengthMm,
                gross_width_mm = @GrossWidthMm,
                weight_kg      = @WeightKg,
                quality_grade  = @QualityGrade,
                is_remnant     = @IsRemnant,
                bundle_id      = @BundleId,
                warehouse_id   = @WarehouseId,
                rack_location  = @RackLocation,
                price_override = @PriceOverride,
                notes          = @Notes,
                updated_at     = NOW()
            WHERE id = @SlabId AND tenant_id = @SupplierId AND is_active = TRUE
            """;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(sql, new
        {
            SlabId        = slabId,
            SupplierId    = supplierId,
            req.InternalRef,
            req.Barcode,
            req.MaterialType,
            req.MaterialName,
            req.ColorFamily,
            req.Pattern,
            req.OriginCountry,
            req.QuarryName,
            req.LotNumber,
            req.BlockNumber,
            req.ThicknessCm,
            req.Finish,
            req.GrossLengthMm,
            req.GrossWidthMm,
            req.WeightKg,
            req.QualityGrade,
            req.IsRemnant,
            req.BundleId,
            req.WarehouseId,
            req.RackLocation,
            req.PriceOverride,
            req.Notes,
        });

        if (affected == 0) { return null; }
        return await GetByIdAsync(supplierId, slabId, ct);
    }

    public async Task<SupplierSlabDto?> UpdateStatusAsync(
        Guid supplierId, Guid slabId, string status, CancellationToken ct = default)
    {
        const string updateSql = """
            UPDATE slabs
            SET status         = @Status,
                status_changed = NOW(),
                updated_at     = NOW()
            WHERE id = @SlabId AND tenant_id = @SupplierId AND is_active = TRUE
            """;

        const string logSql = """
            INSERT INTO slab_events (tenant_id, slab_id, event_type, new_value)
            VALUES (@SupplierId, @SlabId, 'status_change', @Status)
            """;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var txn  = conn.BeginTransaction();
        var affected = await conn.ExecuteAsync(updateSql, new { SlabId = slabId, SupplierId = supplierId, Status = status }, txn);
        if (affected == 0)
        {
            txn.Rollback();
            return null;
        }
        await conn.ExecuteAsync(logSql, new { SlabId = slabId, SupplierId = supplierId, Status = status }, txn);
        txn.Commit();
        return await GetByIdAsync(supplierId, slabId, ct);
    }

    public async Task<SupplierSlabDto?> SetPriceOverrideAsync(
        Guid supplierId, Guid slabId, decimal? price, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE slabs
            SET price_override = @Price,
                updated_at     = NOW()
            WHERE id = @SlabId AND tenant_id = @SupplierId AND is_active = TRUE
            """;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(sql, new { SlabId = slabId, SupplierId = supplierId, Price = price });
        if (affected == 0) { return null; }
        return await GetByIdAsync(supplierId, slabId, ct);
    }

    public async Task<int> BulkUpdateStatusAsync(
        Guid supplierId, IReadOnlyList<Guid> slabIds, string status, CancellationToken ct = default)
    {
        if (slabIds.Count == 0) { return 0; }

        const string updateSql = """
            UPDATE slabs
            SET status         = @Status,
                status_changed = NOW(),
                updated_at     = NOW()
            WHERE id = ANY(@SlabIds) AND tenant_id = @SupplierId AND is_active = TRUE
            """;

        const string logSql = """
            INSERT INTO slab_events (tenant_id, slab_id, event_type, new_value)
            SELECT @SupplierId, s.id, 'status_change', @Status
            FROM   slabs s
            WHERE  s.id = ANY(@SlabIds) AND s.tenant_id = @SupplierId AND s.is_active = TRUE
            """;

        var arr = slabIds.ToArray();
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var txn  = conn.BeginTransaction();
        var affected = await conn.ExecuteAsync(updateSql, new { SlabIds = arr, SupplierId = supplierId, Status = status }, txn);
        if (affected > 0)
        {
            await conn.ExecuteAsync(logSql, new { SlabIds = arr, SupplierId = supplierId, Status = status }, txn);
        }
        txn.Commit();
        return affected;
    }

    public async Task<bool> DeleteAsync(
        Guid supplierId, Guid slabId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE slabs
            SET is_active  = FALSE,
                updated_at = NOW()
            WHERE id = @SlabId AND tenant_id = @SupplierId AND is_active = TRUE
            """;

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteAsync(sql, new { SlabId = slabId, SupplierId = supplierId }) > 0;
    }

    // ── Dapper row mapping ─────────────────────────────────────────────────────
    private sealed record SupplierSlabRow
    {
        public Guid            Id              { get; init; }
        public Guid            VariantId       { get; init; }
        public Guid?           BundleId        { get; init; }
        public string          InternalRef     { get; init; } = string.Empty;
        public string?         Barcode         { get; init; }
        public string          MaterialType    { get; init; } = string.Empty;
        public string          MaterialName    { get; init; } = string.Empty;
        public string?         ColorFamily     { get; init; }
        public string?         Pattern         { get; init; }
        public string?         OriginCountry   { get; init; }
        public string?         QuarryName      { get; init; }
        public string?         LotNumber       { get; init; }
        public string?         BlockNumber     { get; init; }
        public decimal         ThicknessCm     { get; init; }
        public string          Finish          { get; init; } = string.Empty;
        public int             GrossLengthMm   { get; init; }
        public int             GrossWidthMm    { get; init; }
        public decimal         NetSqft         { get; init; }
        public decimal         NetSqm          { get; init; }
        public decimal?        WeightKg        { get; init; }
        public string          QualityGrade    { get; init; } = string.Empty;
        public bool            IsRemnant       { get; init; }
        public string          Status          { get; init; } = string.Empty;
        public DateTimeOffset? StatusChanged   { get; init; }
        public Guid?           ReservedForPoId { get; init; }
        public decimal         BasePrice       { get; init; }
        public decimal?        PriceOverride   { get; init; }
        public string          Currency        { get; init; } = "USD";
        public string?         RackLocation    { get; init; }
        public string?         Notes           { get; init; }
        public Guid?           WarehouseId     { get; init; }
        public string?         WarehouseName   { get; init; }
        public string?         WarehouseCity   { get; init; }
        public string?         WarehouseState  { get; init; }
        public string?         BundleRef       { get; init; }
        public string?         PrimaryPhotoUrl { get; init; }
        public string?         PrimaryThumbUrl { get; init; }
        public int             PhotoCount      { get; init; }
        public DateTimeOffset  CreatedAt       { get; init; }
        public DateTimeOffset  UpdatedAt       { get; init; }
    }

    private static SupplierSlabDto MapToDto(SupplierSlabRow row) => new()
    {
        Id              = row.Id,
        VariantId       = row.VariantId,
        BundleId        = row.BundleId,
        InternalRef     = row.InternalRef,
        Barcode         = row.Barcode,
        MaterialType    = row.MaterialType,
        MaterialName    = row.MaterialName,
        ColorFamily     = row.ColorFamily,
        Pattern         = row.Pattern,
        OriginCountry   = row.OriginCountry,
        QuarryName      = row.QuarryName,
        LotNumber       = row.LotNumber,
        BlockNumber     = row.BlockNumber,
        ThicknessCm     = row.ThicknessCm,
        Finish          = row.Finish,
        GrossLengthMm   = row.GrossLengthMm,
        GrossWidthMm    = row.GrossWidthMm,
        NetSqft         = row.NetSqft,
        NetSqm          = row.NetSqm,
        WeightKg        = row.WeightKg,
        QualityGrade    = row.QualityGrade,
        IsRemnant       = row.IsRemnant,
        Status          = row.Status,
        StatusChanged   = row.StatusChanged,
        ReservedForPoId = row.ReservedForPoId,
        BasePrice       = row.BasePrice,
        PriceOverride   = row.PriceOverride,
        Currency        = row.Currency,
        RackLocation    = row.RackLocation,
        Notes           = row.Notes,
        WarehouseId     = row.WarehouseId,
        WarehouseName   = row.WarehouseName,
        WarehouseCity   = row.WarehouseCity,
        WarehouseState  = row.WarehouseState,
        BundleRef       = row.BundleRef,
        PrimaryPhotoUrl = row.PrimaryPhotoUrl,
        PrimaryThumbUrl = row.PrimaryThumbUrl,
        PhotoCount      = row.PhotoCount,
        CreatedAt       = row.CreatedAt,
        UpdatedAt       = row.UpdatedAt,
    };
}
