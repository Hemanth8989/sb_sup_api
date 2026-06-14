using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using StoneBridge.Application.Catalog.DTOs;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;

namespace StoneBridge.Infrastructure.Data.Repositories;

/// <summary>
/// Dapper-based implementation of ICatalogRepository.
/// Executes parameterised PostgreSQL queries against the slabs + related tables.
/// All queries enforce:
///   1. status = 'available' AND is_active = TRUE on the slab
///   2. EXISTS(active connection between fabricatorId and the slab's supplier)
///   3. PostgreSQL RLS (app.tenant_id session var set by DbConnectionFactory)
/// Dynamic WHERE clauses are built safely — only parameter names are interpolated,
/// never user values.
/// </summary>
public sealed class CatalogRepository : ICatalogRepository
{
    private readonly IDbConnectionFactory         _connectionFactory;
    private readonly ILogger<CatalogRepository>   _logger;

    public CatalogRepository(
        IDbConnectionFactory       connectionFactory,
        ILogger<CatalogRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger            = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResult<CatalogSlabDto>> SearchAsync(
        Guid                fabricatorId,
        CatalogSearchParams searchParams,
        CancellationToken   ct = default)
    {
        var (whereClause, parameters) = BuildWhereClause(fabricatorId, searchParams);
        var orderClause               = BuildOrderClause(searchParams.SortBy, searchParams.SortDir);
        var offset                    = (searchParams.Page - 1) * searchParams.PerPage;

        parameters.Add("PerPage", searchParams.PerPage);
        parameters.Add("Offset",  offset);

        // ── Count query ────────────────────────────────────────────────────
        var countSql = $"""
            SELECT COUNT(*)
            FROM   slabs s
            JOIN   product_variants v  ON v.id        = s.variant_id
            JOIN   products p          ON p.id        = v.product_id
            JOIN   supplier_profiles sp ON sp.tenant_id = s.tenant_id
            LEFT JOIN warehouses w     ON w.id        = s.warehouse_id
            {whereClause}
            """;

        // ── Data query ─────────────────────────────────────────────────────
        var dataSql = $"""
            SELECT
                s.id,
                s.variant_id,
                s.tenant_id          AS supplier_id,
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
                s.updated_at,
                COALESCE(s.price_override, v.base_price) AS list_price,
                v.currency,
                sp.display_name      AS supplier_name,
                sp.verified          AS supplier_verified,
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
            JOIN   product_variants v   ON v.id         = s.variant_id
            JOIN   products p           ON p.id         = v.product_id
            JOIN   supplier_profiles sp ON sp.tenant_id = s.tenant_id
            LEFT JOIN warehouses w      ON w.id         = s.warehouse_id
            {whereClause}
            {orderClause}
            LIMIT  @PerPage OFFSET @Offset
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (totalCount == 0)
        {
            _logger.LogDebug(
                "Catalog search returned 0 results for fabricator {FabricatorId}", fabricatorId);

            return PagedResult<CatalogSlabDto>.Empty(searchParams.Page, searchParams.PerPage);
        }

        var rows = await connection.QueryAsync<CatalogSlabRow>(dataSql, parameters);

        var items = rows.Select(MapToDto).ToList();

        return PagedResult<CatalogSlabDto>.Create(
            items,
            totalCount,
            searchParams.Page,
            searchParams.PerPage);
    }

    // ── WHERE clause builder ───────────────────────────────────────────────────
    private static (string whereClause, DynamicParameters parameters) BuildWhereClause(
        Guid                fabricatorId,
        CatalogSearchParams s)
    {
        var sb     = new StringBuilder();
        var p      = new DynamicParameters();

        // Base conditions — always applied
        // Connection JOIN: only slabs from active connected suppliers
        sb.Append("""
            WHERE s.status    = 'available'
              AND s.is_active = TRUE
              AND p.is_active = TRUE
              AND EXISTS (
                  SELECT 1
                  FROM   connections c
                  WHERE  c.fabricator_id = @FabricatorId
                    AND  c.supplier_id   = s.tenant_id
                    AND  c.status        = 'active'
              )
            """);

        p.Add("FabricatorId", fabricatorId);

        // Supplier filter
        if (s.SupplierId.HasValue)
        {
            sb.Append("\n  AND s.tenant_id = @SupplierId");
            p.Add("SupplierId", s.SupplierId.Value);
        }

        // Material types — multi-value IN filter
        if (s.MaterialTypes.Count > 0)
        {
            var types = s.MaterialTypes
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            if (types.Length > 0)
            {
                sb.Append("\n  AND s.material_type = ANY(@MaterialTypes)");
                p.Add("MaterialTypes", types);
            }
        }

        // Colour families — multi-value IN filter
        if (s.ColorFamilies.Count > 0)
        {
            var colours = s.ColorFamilies
                .Select(c => c.Trim().ToLowerInvariant())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToArray();

            if (colours.Length > 0)
            {
                sb.Append("\n  AND s.color_family = ANY(@ColorFamilies)");
                p.Add("ColorFamilies", colours);
            }
        }

        // Finishes — multi-value IN filter
        if (s.Finishes.Count > 0)
        {
            var finishes = s.Finishes
                .Select(f => f.Trim().ToLowerInvariant())
                .Where(f => !string.IsNullOrEmpty(f))
                .ToArray();

            if (finishes.Length > 0)
            {
                sb.Append("\n  AND s.finish = ANY(@Finishes)");
                p.Add("Finishes", finishes);
            }
        }

        // Thickness range
        if (s.ThicknessMinCm.HasValue)
        {
            sb.Append("\n  AND s.thickness_cm >= @ThicknessMinCm");
            p.Add("ThicknessMinCm", s.ThicknessMinCm.Value);
        }
        if (s.ThicknessMaxCm.HasValue)
        {
            sb.Append("\n  AND s.thickness_cm <= @ThicknessMaxCm");
            p.Add("ThicknessMaxCm", s.ThicknessMaxCm.Value);
        }

        // Price range — uses COALESCE(price_override, base_price)
        if (s.PriceMin.HasValue)
        {
            sb.Append("\n  AND COALESCE(s.price_override, v.base_price) >= @PriceMin");
            p.Add("PriceMin", s.PriceMin.Value);
        }
        if (s.PriceMax.HasValue)
        {
            sb.Append("\n  AND COALESCE(s.price_override, v.base_price) <= @PriceMax");
            p.Add("PriceMax", s.PriceMax.Value);
        }

        // Minimum net square footage
        if (s.MinNetSqft.HasValue)
        {
            sb.Append("\n  AND s.net_sqft >= @MinNetSqft");
            p.Add("MinNetSqft", s.MinNetSqft.Value);
        }

        // Remnant flag
        if (s.IsRemnant.HasValue)
        {
            sb.Append("\n  AND s.is_remnant = @IsRemnant");
            p.Add("IsRemnant", s.IsRemnant.Value);
        }

        // Full-text search using the GENERATED ALWAYS AS search_vector column
        // Falls back to ILIKE when search term is very short (< 3 chars)
        if (!string.IsNullOrWhiteSpace(s.SearchQuery))
        {
            var term = s.SearchQuery.Trim();

            if (term.Length >= 3)
            {
                // Use full-text search for 3+ character terms
                sb.Append("""


                  AND s.search_vector @@ plainto_tsquery('english', @SearchQuery)
                """);
                p.Add("SearchQuery", term);
            }
            else
            {
                // Use ILIKE for very short terms (1-2 chars)
                sb.Append("\n  AND (s.material_name ILIKE @SearchLike OR s.internal_ref ILIKE @SearchLike)");
                p.Add("SearchLike", $"%{term}%");
            }
        }

        return (sb.ToString(), p);
    }

    // ── ORDER BY builder ───────────────────────────────────────────────────────
    private static string BuildOrderClause(string sortBy, string sortDir)
    {
        // Whitelist both sort field and direction to prevent SQL injection
        var dir = string.Equals(sortDir, "ASC", StringComparison.OrdinalIgnoreCase)
            ? "ASC" : "DESC";

        return sortBy.ToLowerInvariant() switch
        {
            "listprice" => $"ORDER BY COALESCE(s.price_override, v.base_price) {dir}",
            "netsqft"   => $"ORDER BY s.net_sqft {dir}",
            _           => $"ORDER BY s.updated_at {dir}",  // default: updatedAt
        };
    }

    // ── Dapper row mapping ─────────────────────────────────────────────────────
    // Internal record — maps exactly to the SELECT column list above
    // Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true handles snake_case → PascalCase
    private sealed record CatalogSlabRow
    {
        public Guid            Id               { get; init; }
        public Guid            VariantId        { get; init; }
        public Guid            SupplierId       { get; init; }
        public Guid?           BundleId         { get; init; }
        public string          InternalRef      { get; init; } = string.Empty;
        public string          MaterialType     { get; init; } = string.Empty;
        public string          MaterialName     { get; init; } = string.Empty;
        public string?         ColorFamily      { get; init; }
        public string?         Pattern          { get; init; }
        public string?         OriginCountry    { get; init; }
        public string?         QuarryName       { get; init; }
        public decimal         ThicknessCm      { get; init; }
        public string          Finish           { get; init; } = string.Empty;
        public int             GrossLengthMm    { get; init; }
        public int             GrossWidthMm     { get; init; }
        public decimal         NetSqft          { get; init; }
        public decimal         NetSqm           { get; init; }
        public string          QualityGrade     { get; init; } = string.Empty;
        public bool            IsRemnant        { get; init; }
        public DateTimeOffset  UpdatedAt        { get; init; }
        public decimal         ListPrice        { get; init; }
        public string          Currency         { get; init; } = "USD";
        public string          SupplierName     { get; init; } = string.Empty;
        public bool            SupplierVerified { get; init; }
        public string?         WarehouseCity    { get; init; }
        public string?         WarehouseState   { get; init; }
        public string?         PrimaryPhotoUrl  { get; init; }
        public string?         PrimaryThumbUrl  { get; init; }
        public int             PhotoCount       { get; init; }
    }

    private static CatalogSlabDto MapToDto(CatalogSlabRow row) => new()
    {
        Id               = row.Id,
        VariantId        = row.VariantId,
        SupplierId       = row.SupplierId,
        BundleId         = row.BundleId,
        InternalRef      = row.InternalRef,
        MaterialType     = row.MaterialType,
        MaterialName     = row.MaterialName,
        ColorFamily      = row.ColorFamily,
        Pattern          = row.Pattern,
        OriginCountry    = row.OriginCountry,
        QuarryName       = row.QuarryName,
        ThicknessCm      = row.ThicknessCm,
        Finish           = row.Finish,
        GrossLengthMm    = row.GrossLengthMm,
        GrossWidthMm     = row.GrossWidthMm,
        NetSqft          = row.NetSqft,
        NetSqm           = row.NetSqm,
        QualityGrade     = row.QualityGrade,
        IsRemnant        = row.IsRemnant,
        UpdatedAt        = row.UpdatedAt,
        ListPrice        = row.ListPrice,
        Currency         = row.Currency,
        SupplierName     = row.SupplierName,
        SupplierVerified = row.SupplierVerified,
        WarehouseCity    = row.WarehouseCity,
        WarehouseState   = row.WarehouseState,
        PrimaryPhotoUrl  = row.PrimaryPhotoUrl,
        PrimaryThumbUrl  = row.PrimaryThumbUrl,
        PhotoCount       = row.PhotoCount,
    };
}