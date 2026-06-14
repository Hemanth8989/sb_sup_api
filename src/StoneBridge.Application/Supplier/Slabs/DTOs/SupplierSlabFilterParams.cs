namespace StoneBridge.Application.Supplier.Slabs.DTOs;

/// <summary>
/// All filter, sort, and pagination parameters for the supplier's inventory list.
/// Unlike the catalog (fabricator side), suppliers can filter by slab status and
/// see all their slabs regardless of availability.
/// All filter fields are optional — null or empty means no filter applied.
/// </summary>
public sealed record SupplierSlabFilterParams
{
    // ── Full-text search ──────────────────────────────────────────────────────
    /// <summary>
    /// Free-text search across material_name, internal_ref, quarry_name.
    /// Uses PostgreSQL search_vector with GIN index for 3+ character terms.
    /// Falls back to ILIKE for shorter terms.
    /// </summary>
    public string? SearchQuery { get; init; }

    // ── Multi-value filters ───────────────────────────────────────────────────
    /// <summary>
    /// Filter by one or more slab statuses. Empty = all statuses.
    /// Valid values: available, reserved, allocated, shipped, hold, sold.
    /// </summary>
    public IReadOnlyList<string> Statuses { get; init; } = [];

    /// <summary>Filter by one or more material types. Empty = all materials.</summary>
    public IReadOnlyList<string> MaterialTypes { get; init; } = [];

    /// <summary>Filter by one or more colour families. Empty = all colours.</summary>
    public IReadOnlyList<string> ColorFamilies { get; init; } = [];

    /// <summary>Filter by one or more finishes. Empty = all finishes.</summary>
    public IReadOnlyList<string> Finishes { get; init; } = [];

    // ── Range filters ─────────────────────────────────────────────────────────
    public decimal? ThicknessMinCm { get; init; }
    public decimal? ThicknessMaxCm { get; init; }
    public decimal? MinNetSqft     { get; init; }

    // ── Boolean flags ─────────────────────────────────────────────────────────
    /// <summary>When true: return only remnants. When false: exclude remnants. When null: return all.</summary>
    public bool? IsRemnant { get; init; }

    // ── Warehouse filter ──────────────────────────────────────────────────────
    /// <summary>Restrict to slabs stored in a specific warehouse.</summary>
    public Guid? WarehouseId { get; init; }

    // ── Sorting ───────────────────────────────────────────────────────────────
    /// <summary>Sort field: updatedAt | status | internalRef | netSqft. Default: updatedAt.</summary>
    public string SortBy  { get; init; } = "updatedAt";

    /// <summary>Sort direction: ASC | DESC. Default: DESC.</summary>
    public string SortDir { get; init; } = "DESC";

    // ── Pagination ────────────────────────────────────────────────────────────
    public int Page    { get; init; } = 1;
    public int PerPage { get; init; } = 50;
}
