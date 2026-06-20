using FluentValidation;

namespace StoneBridge.Application.Supplier.Slabs.Queries.GetSupplierSlabs;

/// <summary>
/// Validates GetSupplierSlabsQuery before the handler executes.
/// Called automatically by ValidationBehaviour.
/// </summary>
public sealed class GetSupplierSlabsQueryValidator
    : AbstractValidator<GetSupplierSlabsQuery>
{
    private static readonly string[] ValidSortFields =
        ["updatedAt", "status", "internalRef", "netSqft", "createdAt",
         "updated_at", "internal_ref", "net_sqft", "created_at",
         "material_name", "materialName", "effective_price", "effectivePrice"];

    private static readonly string[] ValidSortDirs = ["ASC", "DESC"];

    private static readonly string[] ValidStatuses =
        ["available", "reserved", "allocated", "shipped", "hold", "sold"];

    public GetSupplierSlabsQueryValidator()
    {
        RuleFor(x => x.FilterParams.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.FilterParams.PerPage)
            .InclusiveBetween(1, 100)
            .WithMessage("PerPage must be between 1 and 100.");

        RuleFor(x => x.FilterParams.SearchQuery)
            .MaximumLength(200)
            .When(x => x.FilterParams.SearchQuery is not null)
            .WithMessage("Search query must not exceed 200 characters.");

        RuleFor(x => x.FilterParams.Statuses)
            .Must(statuses => statuses.All(s =>
                ValidStatuses.Contains(s.Trim().ToLowerInvariant())))
            .When(x => x.FilterParams.Statuses.Count > 0)
            .WithMessage($"Invalid status value. Valid values: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.FilterParams.ThicknessMinCm)
            .GreaterThan(0)
            .When(x => x.FilterParams.ThicknessMinCm.HasValue)
            .WithMessage("Minimum thickness must be greater than 0.");

        RuleFor(x => x.FilterParams.ThicknessMaxCm)
            .GreaterThan(0)
            .LessThanOrEqualTo(10)
            .When(x => x.FilterParams.ThicknessMaxCm.HasValue)
            .WithMessage("Maximum thickness must be between 0 and 10 cm.");

        RuleFor(x => x)
            .Must(x => !x.FilterParams.ThicknessMinCm.HasValue ||
                       !x.FilterParams.ThicknessMaxCm.HasValue ||
                       x.FilterParams.ThicknessMinCm <= x.FilterParams.ThicknessMaxCm)
            .WithMessage("Minimum thickness must not exceed maximum thickness.");

        RuleFor(x => x.FilterParams.MinNetSqft)
            .GreaterThan(0)
            .When(x => x.FilterParams.MinNetSqft.HasValue)
            .WithMessage("Minimum net square footage must be greater than 0.");

        RuleFor(x => x.FilterParams.SortBy)
            .Must(s => ValidSortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}.");

        RuleFor(x => x.FilterParams.SortDir)
            .Must(d => ValidSortDirs.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDir must be ASC or DESC.");
    }
}
