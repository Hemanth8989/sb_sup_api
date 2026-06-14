using FluentAssertions;
using FluentValidation.TestHelper;
using StoneBridge.Application.Catalog.DTOs;
using StoneBridge.Application.Catalog.Queries.SearchCatalogSlabs;
using Xunit;

namespace StoneBridge.Application.Tests.Catalog;

/// <summary>
/// Unit tests for SearchCatalogSlabsQueryValidator.
/// Uses FluentValidation.TestHelper for precise per-property assertions.
/// Every validation rule has at least one passing and one failing test case.
/// </summary>
public sealed class SearchCatalogSlabsQueryValidatorTests
{
    private readonly SearchCatalogSlabsQueryValidator _validator = new();

    // ── Valid cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithDefaultParams_PassesAllRules()
    {
        // Arrange
        var query = new SearchCatalogSlabsQuery(new CatalogSearchParams());

        // Act & Assert
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithAllFiltersSet_PassesAllRules()
    {
        // Arrange
        var query = new SearchCatalogSlabsQuery(new CatalogSearchParams
        {
            SearchQuery    = "Calacatta",
            MaterialTypes  = new[] { "marble" }.ToList().AsReadOnly(),
            ColorFamilies  = new[] { "white" }.ToList().AsReadOnly(),
            Finishes       = new[] { "polished" }.ToList().AsReadOnly(),
            ThicknessMinCm = 2m,
            ThicknessMaxCm = 3m,
            PriceMin       = 100m,
            PriceMax       = 500m,
            MinNetSqft     = 40m,
            IsRemnant      = false,
            SortBy         = "listPrice",
            SortDir        = "ASC",
            Page           = 2,
            PerPage        = 50,
        });

        // Act & Assert
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── Pagination ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenPageLessThanOne_FailsValidation(int page)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { Page = page });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.Page");
    }

    [Fact]
    public void Validate_WhenPageIsOne_PassesValidation()
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { Page = 1 });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(1000)]
    public void Validate_WhenPerPageOutOfRange_FailsValidation(int perPage)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { PerPage = perPage });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.PerPage");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(100)]
    public void Validate_WhenPerPageInRange_PassesValidation(int perPage)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { PerPage = perPage });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.PerPage");
    }

    // ── SearchQuery ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenSearchQueryExceeds200Chars_FailsValidation()
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { SearchQuery = new string('a', 201) });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.SearchQuery");
    }

    [Fact]
    public void Validate_WhenSearchQueryIsNull_PassesValidation()
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { SearchQuery = null });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.SearchQuery");
    }

    [Fact]
    public void Validate_WhenSearchQueryIs200Chars_PassesValidation()
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { SearchQuery = new string('a', 200) });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.SearchQuery");
    }

    // ── Thickness ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenThicknessMinIsNotPositive_FailsValidation(decimal min)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { ThicknessMinCm = min });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.ThicknessMinCm");
    }

    [Theory]
    [InlineData(10.01)]
    [InlineData(50)]
    public void Validate_WhenThicknessMaxExceeds10_FailsValidation(decimal max)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { ThicknessMaxCm = max });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.ThicknessMaxCm");
    }

    [Fact]
    public void Validate_WhenThicknessMinExceedsMax_FailsValidation()
    {
        var query = new SearchCatalogSlabsQuery(new CatalogSearchParams
        {
            ThicknessMinCm = 3m,
            ThicknessMaxCm = 2m,   // min > max — invalid
        });
        var result = _validator.TestValidate(query);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_WhenThicknessMinEqualsMax_PassesValidation()
    {
        var query = new SearchCatalogSlabsQuery(new CatalogSearchParams
        {
            ThicknessMinCm = 3m,
            ThicknessMaxCm = 3m,   // min == max — valid (single thickness filter)
        });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.ThicknessMinCm");
        result.ShouldNotHaveValidationErrorFor("SearchParams.ThicknessMaxCm");
    }

    // ── Price ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenPriceMinIsNegative_FailsValidation(decimal min)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { PriceMin = min });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.PriceMin");
    }

    [Fact]
    public void Validate_WhenPriceMinIsZero_PassesValidation()
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { PriceMin = 0m });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.PriceMin");
    }

    [Fact]
    public void Validate_WhenPriceMinExceedsMax_FailsValidation()
    {
        var query = new SearchCatalogSlabsQuery(new CatalogSearchParams
        {
            PriceMin = 500m,
            PriceMax = 100m,   // min > max — invalid
        });
        var result = _validator.TestValidate(query);
        result.ShouldHaveAnyValidationError();
    }

    // ── Sort ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("unknownField")]
    [InlineData("createdAt")]
    [InlineData("name")]
    public void Validate_WhenSortByIsInvalid_FailsValidation(string sortBy)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { SortBy = sortBy });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.SortBy");
    }

    [Theory]
    [InlineData("updatedAt")]
    [InlineData("listPrice")]
    [InlineData("netSqft")]
    [InlineData("UPDATEDAT")]  // case-insensitive
    public void Validate_WhenSortByIsValid_PassesValidation(string sortBy)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { SortBy = sortBy });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.SortBy");
    }

    [Theory]
    [InlineData("ASCENDING")]
    [InlineData("DESCENDING")]
    [InlineData("asc ")]  // trailing space — invalid
    public void Validate_WhenSortDirIsInvalid_FailsValidation(string sortDir)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { SortDir = sortDir });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.SortDir");
    }

    [Theory]
    [InlineData("ASC")]
    [InlineData("DESC")]
    [InlineData("asc")]   // lowercase — valid (validator is case-insensitive)
    [InlineData("desc")]
    public void Validate_WhenSortDirIsValid_PassesValidation(string sortDir)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { SortDir = sortDir });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.SortDir");
    }

    // ── MinNetSqft ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenMinNetSqftIsNotPositive_FailsValidation(decimal value)
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { MinNetSqft = value });
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("SearchParams.MinNetSqft");
    }

    [Fact]
    public void Validate_WhenMinNetSqftIsNull_PassesValidation()
    {
        var query  = new SearchCatalogSlabsQuery(new CatalogSearchParams { MinNetSqft = null });
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor("SearchParams.MinNetSqft");
    }
}