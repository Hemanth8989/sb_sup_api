using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class BundleRepository : IBundleRepository
{
    private readonly IDbConnectionFactory _db;

    public BundleRepository(IDbConnectionFactory db) => _db = db;

    // ── List ───────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<BundleDto>> GetAllAsync(
        Guid tenantId, string? search = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                sb.id, sb.bundle_ref, sb.material_name, sb.quarry_name,
                sb.origin_country, sb.arrival_date, sb.invoice_ref, sb.notes,
                sb.slab_count, sb.active_count, sb.created_at, sb.updated_at,
                COALESCE(agg.available_count,     0)    AS available_count,
                agg.total_sqft_available
            FROM slab_bundles sb
            LEFT JOIN (
                SELECT
                    bundle_id,
                    COUNT(*)          FILTER (WHERE status = 'available' AND is_active) AS available_count,
                    SUM(net_sqft)     FILTER (WHERE status = 'available' AND is_active) AS total_sqft_available
                FROM slabs
                WHERE bundle_id IS NOT NULL AND tenant_id = @tenantId
                GROUP BY bundle_id
            ) agg ON agg.bundle_id = sb.id
            WHERE sb.tenant_id = @tenantId
              AND (@search IS NULL
                   OR sb.bundle_ref    ILIKE '%' || @search || '%'
                   OR sb.material_name ILIKE '%' || @search || '%'
                   OR sb.quarry_name   ILIKE '%' || @search || '%')
            ORDER BY sb.updated_at DESC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<BundleRow>(sql, new { tenantId, search });
        return rows.Select(MapToDto).ToList();
    }

    // ── Detail with slabs ──────────────────────────────────────────────────
    public async Task<BundleDetailDto?> GetByIdAsync(
        Guid tenantId, Guid bundleId, CancellationToken ct = default)
    {
        const string bundleSql = """
            SELECT
                sb.id, sb.bundle_ref, sb.material_name, sb.quarry_name,
                sb.origin_country, sb.arrival_date, sb.invoice_ref, sb.notes,
                sb.slab_count, sb.active_count, sb.created_at, sb.updated_at,
                COALESCE(agg.available_count, 0)  AS available_count,
                agg.total_sqft_available
            FROM slab_bundles sb
            LEFT JOIN (
                SELECT bundle_id,
                    COUNT(*) FILTER (WHERE status = 'available' AND is_active) AS available_count,
                    SUM(net_sqft) FILTER (WHERE status = 'available' AND is_active) AS total_sqft_available
                FROM slabs WHERE bundle_id = @bundleId AND tenant_id = @tenantId GROUP BY bundle_id
            ) agg ON agg.bundle_id = sb.id
            WHERE sb.tenant_id = @tenantId AND sb.id = @bundleId
            """;

        const string slabsSql = """
            SELECT
                s.id, s.internal_ref, s.block_number,
                s.thickness_cm, s.finish,
                s.gross_length_mm, s.gross_width_mm, s.net_sqft,
                s.quality_grade, s.status, s.rack_location,
                s.price_override, s.updated_at,
                w.name AS warehouse_name,
                ph.url AS primary_photo_url
            FROM slabs s
            LEFT JOIN warehouses w ON w.id = s.warehouse_id
            LEFT JOIN LATERAL (
                SELECT url FROM slab_photos
                WHERE slab_id = s.id
                ORDER BY sort_order ASC LIMIT 1
            ) ph ON TRUE
            WHERE s.bundle_id = @bundleId AND s.is_active = TRUE AND s.tenant_id = @tenantId
            ORDER BY s.internal_ref ASC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);

        var bundle = await conn.QuerySingleOrDefaultAsync<BundleRow>(
            bundleSql, new { tenantId, bundleId });

        if (bundle is null)
        {
            return null;
        }

        var slabs = await conn.QueryAsync<SlabRow>(slabsSql, new { tenantId, bundleId });

        return new BundleDetailDto
        {
            Id                 = bundle.Id,
            BundleRef          = bundle.BundleRef,
            MaterialName       = bundle.MaterialName,
            QuarryName         = bundle.QuarryName,
            OriginCountry      = bundle.OriginCountry,
            ArrivalDate        = bundle.ArrivalDate,
            InvoiceRef         = bundle.InvoiceRef,
            Notes              = bundle.Notes,
            SlabCount          = bundle.SlabCount,
            ActiveCount        = bundle.ActiveCount,
            AvailableCount     = bundle.AvailableCount,
            TotalSqftAvailable = bundle.TotalSqftAvailable,
            CreatedAt          = bundle.CreatedAt,
            UpdatedAt          = bundle.UpdatedAt,
            Slabs              = slabs.Select(MapSlabToDto).ToList(),
        };
    }

    // ── Create ─────────────────────────────────────────────────────────────
    public async Task<BundleDto> CreateAsync(
        Guid tenantId, CreateBundleRequest req, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO slab_bundles (
                tenant_id, bundle_ref, material_name, quarry_name,
                origin_country, arrival_date, invoice_ref, notes
            ) VALUES (
                @tenantId, @bundleRef, @materialName, @quarryName,
                UPPER(@originCountry), @arrivalDate, @invoiceRef, @notes
            )
            RETURNING id, bundle_ref, material_name, quarry_name,
                      origin_country, arrival_date, invoice_ref, notes,
                      slab_count, active_count, created_at, updated_at
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleAsync<BundleRow>(sql, new
        {
            tenantId,
            bundleRef     = req.BundleRef,
            materialName  = req.MaterialName,
            quarryName    = req.QuarryName,
            originCountry = req.OriginCountry,
            arrivalDate   = req.ArrivalDate,
            invoiceRef    = req.InvoiceRef,
            notes         = req.Notes,
        });

        return MapToDto(row);
    }

    // ── Update ─────────────────────────────────────────────────────────────
    public async Task<BundleDto> UpdateAsync(
        Guid tenantId, Guid bundleId, UpdateBundleRequest req, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE slab_bundles SET
                bundle_ref    = @bundleRef,
                material_name = @materialName,
                quarry_name   = @quarryName,
                origin_country = UPPER(COALESCE(@originCountry, origin_country)),
                arrival_date  = @arrivalDate,
                invoice_ref   = @invoiceRef,
                notes         = @notes,
                updated_at    = NOW()
            WHERE tenant_id = @tenantId AND id = @bundleId
            RETURNING id, bundle_ref, material_name, quarry_name,
                      origin_country, arrival_date, invoice_ref, notes,
                      slab_count, active_count, created_at, updated_at
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<BundleRow>(sql, new
        {
            tenantId,
            bundleId,
            bundleRef     = req.BundleRef,
            materialName  = req.MaterialName,
            quarryName    = req.QuarryName,
            originCountry = req.OriginCountry,
            arrivalDate   = req.ArrivalDate,
            invoiceRef    = req.InvoiceRef,
            notes         = req.Notes,
        });

        return MapToDto(row!);
    }

    // ── Mapping helpers ────────────────────────────────────────────────────
    private static BundleDto MapToDto(BundleRow r) => new()
    {
        Id                 = r.Id,
        BundleRef          = r.BundleRef,
        MaterialName       = r.MaterialName,
        QuarryName         = r.QuarryName,
        OriginCountry      = r.OriginCountry,
        ArrivalDate        = r.ArrivalDate,
        InvoiceRef         = r.InvoiceRef,
        Notes              = r.Notes,
        SlabCount          = r.SlabCount,
        ActiveCount        = r.ActiveCount,
        AvailableCount     = r.AvailableCount,
        TotalSqftAvailable = r.TotalSqftAvailable,
        CreatedAt          = r.CreatedAt,
        UpdatedAt          = r.UpdatedAt,
    };

    private static BundleSlabDto MapSlabToDto(SlabRow r) => new()
    {
        Id              = r.Id,
        InternalRef     = r.InternalRef,
        BlockNumber     = r.BlockNumber,
        ThicknessCm     = r.ThicknessCm,
        Finish          = r.Finish,
        GrossLengthMm   = r.GrossLengthMm,
        GrossWidthMm    = r.GrossWidthMm,
        NetSqft         = r.NetSqft,
        QualityGrade    = r.QualityGrade,
        Status          = r.Status,
        RackLocation    = r.RackLocation,
        WarehouseName   = r.WarehouseName,
        PriceOverride   = r.PriceOverride,
        PrimaryPhotoUrl = r.PrimaryPhotoUrl,
        UpdatedAt       = r.UpdatedAt,
    };

    // ── Dapper row models ──────────────────────────────────────────────────
    private sealed class BundleRow
    {
        public Guid      Id                 { get; init; }
        public string    BundleRef          { get; init; } = string.Empty;
        public string    MaterialName       { get; init; } = string.Empty;
        public string?   QuarryName         { get; init; }
        public string?   OriginCountry      { get; init; }
        public DateOnly? ArrivalDate        { get; init; }
        public string?   InvoiceRef         { get; init; }
        public string?   Notes              { get; init; }
        public int       SlabCount          { get; init; }
        public int       ActiveCount        { get; init; }
        public int       AvailableCount     { get; init; }
        public decimal?  TotalSqftAvailable { get; init; }
        public DateTime  CreatedAt          { get; init; }
        public DateTime  UpdatedAt          { get; init; }
    }

    private sealed class SlabRow
    {
        public Guid     Id              { get; init; }
        public string   InternalRef     { get; init; } = string.Empty;
        public string?  BlockNumber     { get; init; }
        public decimal  ThicknessCm     { get; init; }
        public string   Finish          { get; init; } = string.Empty;
        public int      GrossLengthMm   { get; init; }
        public int      GrossWidthMm    { get; init; }
        public decimal  NetSqft         { get; init; }
        public string   QualityGrade    { get; init; } = "A";
        public string   Status          { get; init; } = string.Empty;
        public string?  RackLocation    { get; init; }
        public string?  WarehouseName   { get; init; }
        public decimal? PriceOverride   { get; init; }
        public string?  PrimaryPhotoUrl { get; init; }
        public DateTime UpdatedAt       { get; init; }
    }
}
