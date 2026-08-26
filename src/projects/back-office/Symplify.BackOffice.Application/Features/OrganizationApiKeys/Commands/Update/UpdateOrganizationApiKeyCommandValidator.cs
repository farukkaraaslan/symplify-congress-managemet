using FluentValidation;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Update;

public class UpdateOrganizationApiKeyCommandValidator : AbstractValidator<UpdateOrganizationApiKeyCommand>
{
    public UpdateOrganizationApiKeyCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage(OrganizationApiKeysMessages.InvalidRequest);

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(OrganizationApiKeysMessages.NameRequired)
            .MaximumLength(200).WithMessage(OrganizationApiKeysMessages.NameMaxLength);

        RuleFor(command => command.ExpiresAt)
            .Must(value => !value.HasValue || ToUtc(value.Value) > DateTime.UtcNow)
            .WithMessage(OrganizationApiKeysMessages.ExpiresAtMustBeFuture);

        RuleFor(command => command.Description)
            .MaximumLength(1000).WithMessage(OrganizationApiKeysMessages.DescriptionMaxLength);

        RuleFor(command => command.AllowedIpAddresses)
            .MaximumLength(2000).WithMessage(OrganizationApiKeysMessages.AllowedIpAddressesMaxLength);

        RuleFor(command => command.AllowedDomains)
            .MaximumLength(2000).WithMessage(OrganizationApiKeysMessages.AllowedDomainsMaxLength);

        RuleFor(command => command.Scopes)
            .NotEmpty().WithMessage(OrganizationApiKeysMessages.AtLeastOneScopeRequired);

        RuleForEach(command => command.Scopes)
            .Must(scope => OrganizationApiKeyScopes.All.Contains(scope, StringComparer.OrdinalIgnoreCase))
            .WithMessage(OrganizationApiKeysMessages.InvalidScope);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }
}
