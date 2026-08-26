using FluentValidation;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Create;

public class CreateOrganizationApiKeyCommandValidator : AbstractValidator<CreateOrganizationApiKeyCommand>
{
    public CreateOrganizationApiKeyCommandValidator()
    {
        RuleFor(command => command.OrganizationId)
            .NotEmpty().WithMessage(OrganizationApiKeysMessages.OrganizationRequired);

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(OrganizationApiKeysMessages.NameRequired)
            .MaximumLength(200).WithMessage(OrganizationApiKeysMessages.NameMaxLength);

        RuleFor(command => command.Environment)
            .NotEmpty().WithMessage(OrganizationApiKeysMessages.EnvironmentRequired)
            .MaximumLength(40).WithMessage(OrganizationApiKeysMessages.EnvironmentMaxLength)
            .Must(IsValidEnvironment).WithMessage(OrganizationApiKeysMessages.InvalidEnvironment);

        RuleFor(command => command.KeyType)
            .NotEmpty().WithMessage(OrganizationApiKeysMessages.KeyTypeRequired)
            .MaximumLength(40).WithMessage(OrganizationApiKeysMessages.KeyTypeMaxLength)
            .Must(IsValidKeyType).WithMessage(OrganizationApiKeysMessages.InvalidKeyType);

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

    private static bool IsValidEnvironment(string? value)
    {
        return new[] { "Production", "Sandbox", "Development" }
            .Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsValidKeyType(string? value)
    {
        return new[] { "SecretKey", "PublicKey", "IntegrationKey" }
            .Contains(value, StringComparer.OrdinalIgnoreCase);
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
