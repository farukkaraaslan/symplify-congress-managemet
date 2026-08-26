using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Create;

public class CreateCongressAnnouncementCommandValidator : AbstractValidator<CreateCongressAnnouncementCommand>
{
    public CreateCongressAnnouncementCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressAnnouncementsMessages.CongressRequired);

        RuleFor(command => command.ExternalUrl)
            .MaximumLength(1000)
            .WithMessage(CongressAnnouncementsMessages.ExternalUrlTooLong);

        RuleFor(command => command.AttachmentPath)
            .MaximumLength(1000)
            .WithMessage(CongressAnnouncementsMessages.AttachmentPathTooLong);

        RuleFor(command => command.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage(CongressAnnouncementsMessages.OrderInvalid);

        RuleFor(command => command.Translations)
            .NotEmpty()
            .WithMessage(CongressAnnouncementsMessages.TitleRequired);
    }
}
