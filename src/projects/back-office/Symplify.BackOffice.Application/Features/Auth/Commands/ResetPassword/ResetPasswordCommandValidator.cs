using FluentValidation;
using Symplify.BackOffice.Application.Features.Auth.Constants;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(AuthMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthMessages.EmailInvalid)
            .MaximumLength(256).WithMessage(AuthMessages.EmailInvalid);

        RuleFor(command => command.Token)
            .NotEmpty().WithMessage(AuthMessages.TokenRequired);

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(AuthMessages.PasswordRequired)
            .MinimumLength(8).WithMessage(AuthMessages.PasswordPolicy)
            .MaximumLength(128).WithMessage(AuthMessages.PasswordPolicy)
            .Matches("[A-Z]").WithMessage(AuthMessages.PasswordPolicy)
            .Matches("[0-9]").WithMessage(AuthMessages.PasswordPolicy)
            .Matches("[^a-zA-Z0-9]").WithMessage(AuthMessages.PasswordPolicy);

        RuleFor(command => command.ConfirmPassword)
            .NotEmpty().WithMessage(AuthMessages.ConfirmPasswordRequired)
            .Equal(command => command.Password).WithMessage(AuthMessages.PasswordsDoNotMatch);
    }
}
