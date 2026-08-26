using FluentValidation;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Constants;

namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.Save;

public sealed class SaveOrganizationMailConfigurationCommandValidator : AbstractValidator<SaveOrganizationMailConfigurationCommand>
{
    public SaveOrganizationMailConfigurationCommandValidator()
    {
        RuleFor(command => command.OrganizationId)
            .NotEmpty()
            .WithMessage(OrganizationMailConfigurationsMessages.OrganizationNotFound);

        RuleFor(command => command.Host).NotEmpty().MaximumLength(250);
        RuleFor(command => command.Port).InclusiveBetween(1, 65535);
        RuleFor(command => command.Username).NotEmpty().MaximumLength(250);
        RuleFor(command => command.Password)
            .MaximumLength(500)
            .When(command => !string.IsNullOrWhiteSpace(command.Password));
        RuleFor(command => command.FromEmail).NotEmpty().EmailAddress().MaximumLength(250);
        RuleFor(command => command.FromName).NotEmpty().MaximumLength(250);
        RuleFor(command => command.ReplyToEmail)
            .EmailAddress()
            .MaximumLength(250)
            .When(command => !string.IsNullOrWhiteSpace(command.ReplyToEmail));
        RuleFor(command => command.ReplyToName)
            .MaximumLength(250)
            .When(command => !string.IsNullOrWhiteSpace(command.ReplyToName));
    }
}
