using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;

namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Update;

public class UpdateCongressSliderCommandValidator : AbstractValidator<UpdateCongressSliderCommand>
{
    public UpdateCongressSliderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CongressId).NotEmpty();

        RuleFor(x => x.ImagePath)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.ImagePath));

        RuleFor(x => x)
            .Must(x => x.Image is not null || !string.IsNullOrWhiteSpace(x.ImagePath))
            .WithMessage(CongressSlidersMessages.ImageRequired);
    }
}
