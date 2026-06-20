using FluentValidation;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.CreatePriceList;

public sealed class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    private static readonly string[] ValidTiers = ["standard", "preferred", "vip"];

    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.Tier).NotEmpty().Must(t => ValidTiers.Contains(t))
            .WithMessage($"Tier must be one of: {string.Join(", ", ValidTiers)}");
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
