using FluentValidation;

namespace StoneBridge.Application.Supplier.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Request.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(300).WithMessage("Display name must not exceed 300 characters.");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Request.Description is not null);

        RuleFor(x => x.Request.Website)
            .MaximumLength(300).WithMessage("Website must not exceed 300 characters.")
            .When(x => x.Request.Website is not null);

        RuleFor(x => x.Request.Phone)
            .MaximumLength(30).WithMessage("Phone must not exceed 30 characters.")
            .When(x => x.Request.Phone is not null);

        RuleFor(x => x.Request.EstablishedYear)
            .InclusiveBetween(1800, DateTime.UtcNow.Year)
            .WithMessage($"Established year must be between 1800 and {DateTime.UtcNow.Year}.")
            .When(x => x.Request.EstablishedYear is not null);

        RuleFor(x => x.Request.Country)
            .Length(2).WithMessage("Country must be a 2-letter ISO code (e.g. US).")
            .When(x => x.Request.Country is not null);
    }
}
