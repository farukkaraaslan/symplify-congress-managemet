using FluentValidation;
using Symplify.BackOffice.Application.Features.Organizations.Constants;

namespace Symplify.BackOffice.Application.Features.Organizations.Commands.Update;

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage(OrganizationsMessages.InvalidOrganizationId);

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(OrganizationsMessages.NameRequired)
            .MaximumLength(200).WithMessage(OrganizationsMessages.NameMaxLength);

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage(OrganizationsMessages.CodeRequired)
            .MaximumLength(80).WithMessage(OrganizationsMessages.CodeMaxLength)
            .Matches("^[a-zA-Z0-9-]+$").WithMessage(OrganizationsMessages.InvalidCode);

        RuleFor(command => command.ShortName)
            .NotEmpty().WithMessage(OrganizationsMessages.ShortNameRequired)
            .MaximumLength(80).WithMessage(OrganizationsMessages.ShortNameMaxLength)
            .Matches("^[a-zA-Z0-9-]+$").WithMessage(OrganizationsMessages.InvalidShortName);

        RuleFor(command => command.WebsiteUrl)
            .MaximumLength(500).WithMessage(OrganizationsMessages.WebsiteUrlMaxLength);

        RuleFor(command => command.HostUrl)
            .MaximumLength(500).WithMessage(OrganizationsMessages.HostUrlMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(1000).WithMessage(OrganizationsMessages.DescriptionMaxLength);

        RuleFor(command => command.ContactName)
            .MaximumLength(200).WithMessage(OrganizationsMessages.ContactNameMaxLength);

        RuleFor(command => command.ContactTitle)
            .MaximumLength(200).WithMessage(OrganizationsMessages.ContactTitleMaxLength);

        RuleFor(command => command.ContactEmail)
            .MaximumLength(256).WithMessage(OrganizationsMessages.ContactEmailMaxLength)
            .EmailAddress().WithMessage(OrganizationsMessages.InvalidContactEmail)
            .When(command => !string.IsNullOrWhiteSpace(command.ContactEmail));

        RuleFor(command => command.ContactPhone)
            .MaximumLength(50).WithMessage(OrganizationsMessages.ContactPhoneMaxLength);

        RuleFor(command => command.ContactNote)
            .MaximumLength(1000).WithMessage(OrganizationsMessages.ContactNoteMaxLength);

        RuleFor(command => command.LogoLightPath)
            .MaximumLength(500).WithMessage(OrganizationsMessages.LogoLightPathMaxLength);

        RuleFor(command => command.LogoDarkPath)
            .MaximumLength(500).WithMessage(OrganizationsMessages.LogoDarkPathMaxLength);

        RuleFor(command => command.BrandColor)
            .MaximumLength(20).WithMessage(OrganizationsMessages.BrandColorMaxLength)
            .Must(BeValidBrandColor).WithMessage(OrganizationsMessages.InvalidBrandColor)
            .When(command => !string.IsNullOrWhiteSpace(command.BrandColor));
    }

    private static bool BeValidBrandColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        string color = value.Trim();

        return color.Length == 7 && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit);
    }
}
