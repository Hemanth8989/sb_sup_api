using Bogus;
using StoneBridge.Application.Catalog.DTOs;
using StoneBridge.Application.Common.Models;

namespace StoneBridge.Application.Tests.Common;

/// <summary>
/// Static factory methods for creating realistic test data using Bogus.
/// Use these in test classes to avoid repeating data construction logic.
/// </summary>
public static class FakeData
{
    private static readonly Faker _faker = new("en");

    // ── CatalogSearchParams factories ──────────────────────────────────────────

    public static CatalogSearchParams DefaultSearchParams() => new()
    {
        Page    = 1,
        PerPage = 24,
        SortBy  = "updatedAt",
        SortDir = "DESC",
    };

    public static CatalogSearchParams SearchParamsWithQuery(string query) => new()
    {
        SearchQuery = query,
        Page        = 1,
        PerPage     = 24,
        SortBy      = "updatedAt",
        SortDir     = "DESC",
    };

    public static CatalogSearchParams SearchParamsWithMaterials(params string[] materials) => new()
    {
        MaterialTypes = materials.ToList().AsReadOnly(),
        Page          = 1,
        PerPage       = 24,
        SortBy        = "updatedAt",
        SortDir       = "DESC",
    };

    // ── CatalogSlabDto factories ───────────────────────────────────────────────

    public static CatalogSlabDto CatalogSlab(Guid? supplierId = null) => new()
    {
        Id               = Guid.NewGuid(),
        VariantId        = Guid.NewGuid(),
        SupplierId       = supplierId ?? Guid.NewGuid(),
        BundleId         = null,
        InternalRef      = $"CAL-{_faker.Random.Number(1000, 9999)}",
        MaterialType     = _faker.PickRandom("marble", "granite", "quartzite"),
        MaterialName     = _faker.PickRandom("Calacatta Gold", "Black Galaxy", "Super White", "Statuario"),
        ColorFamily      = _faker.PickRandom("white", "black", "gray"),
        Pattern          = _faker.PickRandom("veined", "solid", "flecked"),
        OriginCountry    = _faker.PickRandom("IT", "BR", "IN"),
        QuarryName       = _faker.Company.CompanyName(),
        ThicknessCm      = _faker.PickRandom(2m, 3m),
        Finish           = _faker.PickRandom("polished", "honed", "leathered"),
        GrossLengthMm    = _faker.Random.Number(2400, 3600),
        GrossWidthMm     = _faker.Random.Number(1400, 2000),
        NetSqft          = _faker.Finance.Amount(40m, 80m),
        NetSqm           = _faker.Finance.Amount(3.5m, 7.5m),
        QualityGrade     = _faker.PickRandom("A", "B"),
        IsRemnant        = false,
        ListPrice        = _faker.Finance.Amount(150m, 600m),
        Currency         = "USD",
        SupplierName     = _faker.Company.CompanyName(),
        SupplierVerified = _faker.Random.Bool(),
        WarehouseCity    = _faker.Address.City(),
        WarehouseState   = _faker.Address.StateAbbr(),
        PrimaryPhotoUrl  = null,
        PrimaryThumbUrl  = null,
        PhotoCount       = 0,
        UpdatedAt        = DateTimeOffset.UtcNow.AddDays(-_faker.Random.Number(0, 30)),
    };

    public static List<CatalogSlabDto> CatalogSlabs(int count, Guid? supplierId = null)
        => Enumerable.Range(0, count).Select(_ => CatalogSlab(supplierId)).ToList();

    /// <summary>Build a PagedResult with realistic test data.</summary>
    public static PagedResult<CatalogSlabDto> PagedCatalogSlabs(
        int  itemCount  = 5,
        int  totalCount = 20,
        int  page       = 1,
        int  perPage    = 24)
        => PagedResult<CatalogSlabDto>.Create(
            CatalogSlabs(itemCount),
            totalCount,
            page,
            perPage);
}