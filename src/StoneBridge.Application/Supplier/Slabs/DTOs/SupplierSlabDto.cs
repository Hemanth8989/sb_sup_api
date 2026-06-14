namespace StoneBridge.Application.Supplier.Slabs.DTOs;

/// <summary>
/// Data returned for a single slab in the supplier's inventory view.
/// Richer than CatalogSlabDto — includes internal-only fields (status, price override,
/// reservation info, notes) that fabricators never see.
/// </summary>
public sealed record SupplierSlabDto
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid   Id          { get; init; }
    public Guid   VariantId   { get; init; }
    public Guid?  BundleId    { get; init; }
    public string InternalRef { get; init; } = string.Empty;

    // ── Material classification ───────────────────────────────────────────────
    public string  MaterialType  { get; init; } = string.Empty;
    public string  MaterialName  { get; init; } = string.Empty;
    public string? ColorFamily   { get; init; }
    public string? Pattern       { get; init; }
    public string? OriginCountry { get; init; }
    public string? QuarryName    { get; init; }

    // ── Physical attributes ───────────────────────────────────────────────────
    public decimal ThicknessCm   { get; init; }
    public string  Finish        { get; init; } = string.Empty;
    public int     GrossLengthMm { get; init; }
    public int     GrossWidthMm  { get; init; }
    public decimal NetSqft       { get; init; }
    public decimal NetSqm        { get; init; }
    public string  QualityGrade  { get; init; } = string.Empty;
    public bool    IsRemnant     { get; init; }

    // ── Status (supplier-only) ────────────────────────────────────────────────
    /// <summary>Current lifecycle status: available | reserved | allocated | shipped | hold | sold.</summary>
    public string  Status        { get; init; } = string.Empty;

    /// <summary>When the status last changed. Useful for tracking how long slabs sit in each state.</summary>
    public DateTimeOffset? StatusChanged { get; init; }

    /// <summary>The PO ID this slab is reserved or allocated against. Null when available or on hold.</summary>
    public Guid? ReservedForPoId { get; init; }

    // ── Pricing (supplier-only) ───────────────────────────────────────────────
    /// <summary>The variant's base price. What all fabricators see unless a price override is set.</summary>
    public decimal  BasePrice     { get; init; }

    /// <summary>
    /// Slab-level price override. When set, fabricators see this instead of BasePrice.
    /// Null means the slab uses the variant's base price.
    /// </summary>
    public decimal? PriceOverride { get; init; }

    /// <summary>Resolved price shown to fabricators: PriceOverride ?? BasePrice.</summary>
    public decimal  EffectivePrice => PriceOverride ?? BasePrice;

    public string Currency { get; init; } = "USD";

    // ── Notes (supplier-only) ─────────────────────────────────────────────────
    /// <summary>Internal notes visible only to the supplier.</summary>
    public string? Notes { get; init; }

    // ── Warehouse location ────────────────────────────────────────────────────
    public Guid?   WarehouseId    { get; init; }
    public string? WarehouseCity  { get; init; }
    public string? WarehouseState { get; init; }

    // ── Photos ────────────────────────────────────────────────────────────────
    public string? PrimaryPhotoUrl { get; init; }
    public string? PrimaryThumbUrl { get; init; }
    public int     PhotoCount      { get; init; }

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
