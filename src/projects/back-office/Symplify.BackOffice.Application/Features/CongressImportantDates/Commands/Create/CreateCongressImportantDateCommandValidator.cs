using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;

namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Create;

public class CreateCongressImportantDateCommandValidator : AbstractValidator<CreateCongressImportantDateCommand>
{
    public CreateCongressImportantDateCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressImportantDatesMessages.CongressRequired);

        RuleFor(command => command.StartDate)
            .NotEmpty()
            .WithMessage(CongressImportantDatesMessages.StartDateRequired);

        RuleFor(command => command.EndDate)
            .NotEmpty()
            .WithMessage(CongressImportantDatesMessages.EndDateRequired)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage(CongressImportantDatesMessages.DateRangeInvalid);

        RuleFor(command => command.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage(CongressImportantDatesMessages.InvalidOrder);

        RuleFor(command => command.Translations)
            .NotEmpty()
            .WithMessage(CongressImportantDatesMessages.DefaultTranslationRequired);
    }
}
