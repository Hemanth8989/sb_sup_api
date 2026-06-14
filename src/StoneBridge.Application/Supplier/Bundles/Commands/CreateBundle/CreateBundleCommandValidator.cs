using FluentValidation;

namespace StoneBridge.Application.Supplier.Bundles.Commands.CreateBundle;

public sealed class CreateBundleCommandValidator : AbstractValidator<CreateBundleCommand>
{
    public CreateBundleCommandValidator()
    {
        RuleFor(x => x.Request.BundleRef)
            .NotEmpty().WithMessage("Bundle reference is required.")
            .MaximumLength(100).WithMessage("Bundle reference must not exceed 100 characters.");

        RuleFor(x => x.Request.MaterialName)
            .NotEmpty().WithMessage("Material name is required.")
            .MaximumLength(200).WithMessage("Material name must not exceed 200 characters.");

        RuleFor(x => x.Request.QuarryName)
            .MaximumLength(200).When(x => x.Request.QuarryName is not null);

        RuleFor(x => x.Request.InvoiceRef)
            .MaximumLength(100).When(x => x.Request.InvoiceRef is not null);
    }
}
