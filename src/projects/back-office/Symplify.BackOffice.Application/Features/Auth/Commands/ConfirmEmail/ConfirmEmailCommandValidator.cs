using FluentValidation;
using Symplify.BackOffice.Application.Features.Auth.Constants;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(AuthMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthMessages.EmailInvalid);

        RuleFor(command => command.Token)
            .NotEmpty().WithMessage(AuthMessages.TokenRequired);
    }
}
