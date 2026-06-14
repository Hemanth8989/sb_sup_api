using FluentValidation;

namespace StoneBridge.Application.Catalog.Queries.SearchCatalogSlabs;

/// <summary>
/// Validates SearchCatalogSlabsQuery before the handler executes.
/// Enforces safe bounds on pagination and range filters.
/// Called automatically by ValidationBehaviour.
/// </summary>
public sealed class SearchCatalogSlabsQueryValidator
    : AbstractValidator<SearchCatalogSlabsQuery>
{
    private static readonly string[] ValidSortFields = ["updatedAt", "listPrice", "netSqft"];
    private static readonly string[] ValidSortDirs   = ["ASC", "DESC"];

    public SearchCatalogSlabsQueryValidator()
    {
        RuleFor(x => x.SearchParams.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.SearchParams.PerPage)
            .InclusiveBetween(1, 100)
            .WithMessage("PerPage must be between 1 and 100.");

        RuleFor(x => x.SearchParams.SearchQuery)
            .MaximumLength(200)
            .When(x => x.SearchParams.SearchQuery is not null)
            .WithMessage("Search query must not exceed 200 characters.");

        RuleFor(x => x.SearchParams.ThicknessMinCm)
            .GreaterThan(0)
            .When(x => x.SearchParams.ThicknessMinCm.HasValue)
            .WithMessage("Minimum thickness must be greater than 0.");

        RuleFor(x => x.SearchParams.ThicknessMaxCm)
            .GreaterThan(0)
            .LessThanOrEqualTo(10)
            .When(x => x.SearchParams.ThicknessMaxCm.HasValue)
            .WithMessage("Maximum thickness must be between 0 and 10 cm.");

        RuleFor(x => x)
            .Must(x => !x.SearchParams.ThicknessMinCm.HasValue ||
                       !x.SearchParams.ThicknessMaxCm.HasValue ||
                       x.SearchParams.ThicknessMinCm <= x.SearchParams.ThicknessMaxCm)
            .WithMessage("Minimum thickness must not exceed maximum thickness.");

        RuleFor(x => x.SearchParams.PriceMin)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SearchParams.PriceMin.HasValue)
            .WithMessage("Minimum price must be 0 or greater.");

        RuleFor(x => x.SearchParams.PriceMax)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SearchParams.PriceMax.HasValue)
            .WithMessage("Maximum price must be 0 or greater.");

        RuleFor(x => x)
            .Must(x => !x.SearchParams.PriceMin.HasValue ||
                       !x.SearchParams.PriceMax.HasValue ||
                       x.SearchParams.PriceMin <= x.SearchParams.PriceMax)
            .WithMessage("Minimum price must not exceed maximum price.");

        RuleFor(x => x.SearchParams.MinNetSqft)
            .GreaterThan(0)
            .When(x => x.SearchParams.MinNetSqft.HasValue)
            .WithMessage("Minimum net square footage must be greater than 0.");

        RuleFor(x => x.SearchParams.SortBy)
            .Must(s => ValidSortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}.");

        RuleFor(x => x.SearchParams.SortDir)
            .Must(d => ValidSortDirs.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDir must be ASC or DESC.");
    }
}