using FluentValidation;
using Symplify.BackOffice.Application.Features.Auth.Constants;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(AuthMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthMessages.EmailInvalid)
            .MaximumLength(256).WithMessage(AuthMessages.EmailInvalid);

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(AuthMessages.PasswordRequired)
            .MaximumLength(256).WithMessage(AuthMessages.EmailInvalid);
    }
}
