using FluentValidation;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.UpsertProduct;

public sealed class UpsertProductCommandValidator : AbstractValidator<UpsertProductCommand>
{
    private static readonly string[] ValidUoms =
    [
        "each", "sqft", "sqm", "linear_ft", "linear_m",
        "liter", "gallon", "kg", "lb", "box", "case",
        "roll", "bag", "pair", "set"
    ];

    public UpsertProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CategoryCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Brand).MaximumLength(150).When(x => x.Brand is not null);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VariantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitOfMeasure).NotEmpty()
            .Must(u => ValidUoms.Contains(u))
            .WithMessage($"UnitOfMeasure must be one of: {string.Join(", ", ValidUoms)}");
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QtyAvailable).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LeadTimeDays).GreaterThanOrEqualTo(0).When(x => x.LeadTimeDays.HasValue);
    }
}
