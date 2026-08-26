using FluentValidation;
using Symplify.BackOffice.Application.Features.Auth.Constants;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(AuthMessages.NameRequired)
            .MaximumLength(100).WithMessage(AuthMessages.RegisterFailed);

        RuleFor(command => command.Surname)
            .NotEmpty().WithMessage(AuthMessages.SurnameRequired)
            .MaximumLength(100).WithMessage(AuthMessages.RegisterFailed);

        RuleFor(command => command.Institution)
            .NotEmpty().WithMessage(AuthMessages.InstitutionRequired)
            .MaximumLength(250).WithMessage(AuthMessages.RegisterFailed);

        RuleFor(command => command.TitleId)
            .NotNull().WithMessage(AuthMessages.TitleRequired)
            .NotEqual(Guid.Empty).WithMessage(AuthMessages.TitleRequired);

        RuleFor(command => command.OrganizationId)
            .NotNull().WithMessage(AuthMessages.OrganizationRequired)
            .NotEqual(Guid.Empty).WithMessage(AuthMessages.OrganizationRequired);

        RuleFor(command => command.CountryId)
            .NotNull().WithMessage(AuthMessages.CountryRequired)
            .NotEqual(Guid.Empty).WithMessage(AuthMessages.CountryRequired);

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage(AuthMessages.PhoneRequired)
            .MaximumLength(50).WithMessage(AuthMessages.PhoneInvalid)
            .Must(BeValidE164Phone).WithMessage(AuthMessages.PhoneInvalid);

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(AuthMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthMessages.EmailInvalid)
            .MaximumLength(256).WithMessage(AuthMessages.EmailInvalid);

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

        RuleFor(command => command.AcceptTerms)
            .Equal(true).WithMessage(AuthMessages.TermsRequired);
    }

    private static bool BeValidE164Phone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = value.Trim();
        if (normalized.StartsWith("+", StringComparison.Ordinal))
            normalized = "+" + new string(normalized[1..].Where(char.IsDigit).ToArray());

        return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\+[1-9]\d{6,14}$");
    }
}
