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
            JOIN   product_variants v ON v.id = s.variant_id
            LEFT JOIN warehouses w    ON w.id = s.warehouse_id
            {whereClause}
            """;

        var dataSql = $"""
            SELECT
                s.id,
                s.variant_id,
                s.bundle_id,
                s.internal_ref,
                s.material_type,
                s.material_name,
                s.color_family,
                s.pattern,
                s.origin_country,
                s.quarry_name,
                s.thickness_cm,
                s.finish,
                s.gross_length_mm,
                s.gross_width_mm,
                s.net_sqft,
                s.net_sqm,
                s.quality_grade,
                s.is_remnant,
                s.status,
                s.status_changed,
                s.reserved_for_po    AS reserved_for_po_id,
                s.price_override,
                s.notes,
                s.warehouse_id,
                s.created_at,
                s.updated_at,
                v.base_price,
                v.currency,
                w.city               AS warehouse_city,
                w.state_province     AS warehouse_state,
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
            JOIN   product_variants v ON v.id = s.variant_id
            LEFT JOIN warehouses w    ON w.id = s.warehouse_id
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
    private sealed record SupplierSlabRow
    {
        public Guid            Id              { get; init; }
        public Guid            VariantId       { get; init; }
        public Guid?           BundleId        { get; init; }
        public string          InternalRef     { get; init; } = string.Empty;
        public string          MaterialType    { get; init; } = string.Empty;
        public string          MaterialName    { get; init; } = string.Empty;
        public string?         ColorFamily     { get; init; }
        public string?         Pattern         { get; init; }
        public string?         OriginCountry   { get; init; }
        public string?         QuarryName      { get; init; }
        public decimal         ThicknessCm     { get; init; }
        public string          Finish          { get; init; } = string.Empty;
        public int             GrossLengthMm   { get; init; }
        public int             GrossWidthMm    { get; init; }
        public decimal         NetSqft         { get; init; }
        public decimal         NetSqm          { get; init; }
        public string          QualityGrade    { get; init; } = string.Empty;
        public bool            IsRemnant       { get; init; }
        public string          Status          { get; init; } = string.Empty;
        public DateTimeOffset? StatusChanged   { get; init; }
        public Guid?           ReservedForPoId { get; init; }
        public decimal         BasePrice       { get; init; }
        public decimal?        PriceOverride   { get; init; }
        public string          Currency        { get; init; } = "USD";
        public string?         Notes           { get; init; }
        public Guid?           WarehouseId     { get; init; }
        public string?         WarehouseCity   { get; init; }
        public string?         WarehouseState  { get; init; }
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
        MaterialType    = row.MaterialType,
        MaterialName    = row.MaterialName,
        ColorFamily     = row.ColorFamily,
        Pattern         = row.Pattern,
        OriginCountry   = row.OriginCountry,
        QuarryName      = row.QuarryName,
        ThicknessCm     = row.ThicknessCm,
        Finish          = row.Finish,
        GrossLengthMm   = row.GrossLengthMm,
        GrossWidthMm    = row.GrossWidthMm,
        NetSqft         = row.NetSqft,
        NetSqm          = row.NetSqm,
        QualityGrade    = row.QualityGrade,
        IsRemnant       = row.IsRemnant,
        Status          = row.Status,
        StatusChanged   = row.StatusChanged,
        ReservedForPoId = row.ReservedForPoId,
        BasePrice       = row.BasePrice,
        PriceOverride   = row.PriceOverride,
        Currency        = row.Currency,
        Notes           = row.Notes,
        WarehouseId     = row.WarehouseId,
        WarehouseCity   = row.WarehouseCity,
        WarehouseState  = row.WarehouseState,
        PrimaryPhotoUrl = row.PrimaryPhotoUrl,
        PrimaryThumbUrl = row.PrimaryThumbUrl,
        PhotoCount      = row.PhotoCount,
        CreatedAt       = row.CreatedAt,
        UpdatedAt       = row.UpdatedAt,
    };
}
