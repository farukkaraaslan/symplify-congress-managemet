using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Constants;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class OrganizationMailConfigurationResolver : IOrganizationMailConfigurationResolver
{
    private readonly IOrganizationMailConfigurationRepository _repository;
    private readonly IMailCredentialProtector _credentialProtector;

    public OrganizationMailConfigurationResolver(
        IOrganizationMailConfigurationRepository repository,
        IMailCredentialProtector credentialProtector)
    {
        _repository = repository;
        _credentialProtector = credentialProtector;
    }

    public async Task<ResolvedOrganizationMailConfiguration> ResolveAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
            throw new BusinessException(OrganizationMailConfigurationsMessages.OrganizationNotFound);

        OrganizationMailConfiguration? entity = await _repository.GetAsync(
            predicate: configuration =>
                configuration.OrganizationId == organizationId &&
                configuration.IsActive,
            cancellationToken: cancellationToken);

        if (entity is null)
            throw new BusinessException(OrganizationMailConfigurationsMessages.ActiveConfigurationNotFound);

        string password;
        try
        {
            password = _credentialProtector.Unprotect(entity.PasswordCipherText);
        }
        catch
        {
            throw new BusinessException(OrganizationMailConfigurationsMessages.PasswordCannotBeDecrypted);
        }

        return new ResolvedOrganizationMailConfiguration
        {
            OrganizationId = entity.OrganizationId,
            Host = entity.Host,
            Port = entity.Port,
            EnableSsl = entity.EnableSsl,
            Username = entity.Username,
            Password = password,
            FromEmail = entity.FromEmail,
            FromName = entity.FromName,
            ReplyToEmail = entity.ReplyToEmail,
            ReplyToName = entity.ReplyToName,
            MailLogoBucketName = entity.MailLogoBucketName,
            MailLogoObjectName = entity.MailLogoObjectName,
            MailLogoContentType = entity.MailLogoContentType,
            MailLogoFileName = entity.MailLogoFileName
        };
    }
}
