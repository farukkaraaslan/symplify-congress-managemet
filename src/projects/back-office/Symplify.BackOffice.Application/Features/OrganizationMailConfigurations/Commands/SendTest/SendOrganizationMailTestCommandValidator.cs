using FluentValidation;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Constants;

namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.SendTest;

public sealed class SendOrganizationMailTestCommandValidator : AbstractValidator<SendOrganizationMailTestCommand>
{
    public SendOrganizationMailTestCommandValidator()
    {
        RuleFor(command => command.OrganizationId)
            .NotEmpty()
            .WithMessage(OrganizationMailConfigurationsMessages.OrganizationNotFound);
        RuleFor(command => command.ToEmail).NotEmpty().EmailAddress().MaximumLength(250);
        RuleFor(command => command.ToName)
            .MaximumLength(250)
            .When(command => !string.IsNullOrWhiteSpace(command.ToName));
    }
}
