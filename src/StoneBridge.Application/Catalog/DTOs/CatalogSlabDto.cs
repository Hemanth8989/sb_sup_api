using StoneBridge.Domain.Enums;

namespace StoneBridge.Application.Catalog.DTOs;

/// <summary>
/// Data returned for a single slab in the fabricator's catalog view.
/// Enriched with supplier name, computed list price, and photo URLs.
/// Excludes internal-only supplier fields (cost, lot number, block number).
/// This DTO is what fabricators see — not what suppliers manage.
/// </summary>
public sealed record CatalogSlabDto
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid    Id          { get; init; }
    public Guid    VariantId   { get; init; }
    public Guid    SupplierId  { get; init; }
    public Guid?   BundleId    { get; init; }

    // ── Supplier info ─────────────────────────────────────────────────────────
    public string  SupplierName     { get; init; } = string.Empty;
    public bool    SupplierVerified { get; init; }

    // ── Material classification ───────────────────────────────────────────────
    public string  InternalRef    { get; init; } = string.Empty;
    public string  MaterialType   { get; init; } = string.Empty;
    public string  MaterialName   { get; init; } = string.Empty;
    public string? ColorFamily    { get; init; }
    public string? Pattern        { get; init; }
    public string? OriginCountry  { get; init; }
    public string? QuarryName     { get; init; }

    // ── Physical attributes ───────────────────────────────────────────────────
    public decimal ThicknessCm   { get; init; }
    public string  Finish        { get; init; } = string.Empty;
    public int     GrossLengthMm { get; init; }
    public int     GrossWidthMm  { get; init; }
    public decimal NetSqft       { get; init; }
    public decimal NetSqm        { get; init; }
    public string  QualityGrade  { get; init; } = string.Empty;
    public bool    IsRemnant     { get; init; }

    // ── Pricing ───────────────────────────────────────────────────────────────
    /// <summary>
    /// The price the fabricator sees: COALESCE(price_override, variant.base_price).
    /// Always set — never null — because every variant has a base_price.
    /// </summary>
    public decimal ListPrice { get; init; }
    public string  Currency  { get; init; } = "USD";

    // ── Photos ────────────────────────────────────────────────────────────────
    /// <summary>Full-size URL of the first photo (sort_order = 0). Null if no photos.</summary>
    public string? PrimaryPhotoUrl { get; init; }

    /// <summary>Thumbnail URL of the first photo. Null if no photos.</summary>
    public string? PrimaryThumbUrl { get; init; }

    /// <summary>Total number of photos for this slab.</summary>
    public int PhotoCount { get; init; }

    // ── Warehouse location ────────────────────────────────────────────────────
    public string? WarehouseCity  { get; init; }
    public string? WarehouseState { get; init; }

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTimeOffset UpdatedAt { get; init; }
}