using FluentValidation;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.AdjustStock;

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Delta must be non-zero.");
        RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0).When(x => x.NewPrice.HasValue);
    }
}
