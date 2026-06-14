using StoneBridge.Domain.Enums;

namespace StoneBridge.Application.Catalog.DTOs;

/// <summary>
/// All filter, sort, and pagination parameters for the slab catalog search.
/// All filter fields are optional — null or empty means no filter applied.
/// Passed from the HTTP query string → endpoint → query → repository.
/// </summary>
public sealed record CatalogSearchParams
{
    // ── Full-text search ──────────────────────────────────────────────────────
    /// <summary>
    /// Free-text search across material_name, quarry_name, lot_number, internal_ref.
    /// Uses PostgreSQL tsvector search_vector column with GIN index.
    /// </summary>
    public string? SearchQuery { get; init; }

    // ── Multi-value filters ───────────────────────────────────────────────────
    /// <summary>Filter by one or more material types. Empty = all materials.</summary>
    public IReadOnlyList<string> MaterialTypes { get; init; } = [];

    /// <summary>Filter by one or more colour families. Empty = all colours.</summary>
    public IReadOnlyList<string> ColorFamilies { get; init; } = [];

    /// <summary>Filter by one or more finishes. Empty = all finishes.</summary>
    public IReadOnlyList<string> Finishes { get; init; } = [];

    // ── Range filters ─────────────────────────────────────────────────────────
    public decimal? ThicknessMinCm { get; init; }
    public decimal? ThicknessMaxCm { get; init; }
    public decimal? PriceMin       { get; init; }
    public decimal? PriceMax       { get; init; }
    public decimal? MinNetSqft     { get; init; }

    // ── Boolean flags ─────────────────────────────────────────────────────────
    /// <summary>When true: return only remnant slabs. When false: exclude remnants. When null: return all.</summary>
    public bool? IsRemnant { get; init; }

    // ── Supplier filter ───────────────────────────────────────────────────────
    /// <summary>Restrict to a specific supplier. Used when fabricator clicks "View this supplier's catalog".</summary>
    public Guid? SupplierId { get; init; }

    // ── Sorting ───────────────────────────────────────────────────────────────
    /// <summary>Sort field: updatedAt | listPrice | netSqft. Default: updatedAt.</summary>
    public string SortBy  { get; init; } = "updatedAt";

    /// <summary>Sort direction: ASC | DESC. Default: DESC.</summary>
    public string SortDir { get; init; } = "DESC";

    // ── Pagination ────────────────────────────────────────────────────────────
    public int Page    { get; init; } = 1;
    public int PerPage { get; init; } = 24;
}