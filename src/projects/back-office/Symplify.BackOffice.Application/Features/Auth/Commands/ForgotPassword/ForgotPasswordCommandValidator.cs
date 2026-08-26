using FluentValidation;
using Symplify.BackOffice.Application.Features.Auth.Constants;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(AuthMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthMessages.EmailInvalid)
            .MaximumLength(256).WithMessage(AuthMessages.EmailInvalid);
    }
}
