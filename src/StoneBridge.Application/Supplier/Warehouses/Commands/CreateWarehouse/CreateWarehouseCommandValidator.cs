using FluentValidation;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.CreateWarehouse;

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Warehouse name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Request.Phone)
            .MaximumLength(30).WithMessage("Phone must not exceed 30 characters.")
            .When(x => x.Request.Phone is not null);

        RuleFor(x => x.Request.Country)
            .Length(2).WithMessage("Country must be a 2-letter ISO code.")
            .When(x => x.Request.Country is not null);
    }
}
