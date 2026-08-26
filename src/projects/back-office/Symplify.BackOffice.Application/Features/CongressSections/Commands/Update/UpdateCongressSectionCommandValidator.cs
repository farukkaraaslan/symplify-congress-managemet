using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;

namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Update;

public class UpdateCongressSectionCommandValidator : AbstractValidator<UpdateCongressSectionCommand>
{
    public UpdateCongressSectionCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage(CongressSectionsMessages.EntityNotFound);

        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressSectionsMessages.CongressRequired);

        RuleFor(command => command.BindingKey)
            .NotEmpty()
            .WithMessage(CongressSectionsMessages.BindingKeyRequired)
            .MaximumLength(100)
            .WithMessage(CongressSectionsMessages.BindingKeyTooLong);

        RuleFor(command => command.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage(CongressSectionsMessages.InvalidOrder);

        RuleFor(command => command.Translations)
            .NotEmpty()
            .WithMessage(CongressSectionsMessages.DefaultTranslationRequired);
    }
}
