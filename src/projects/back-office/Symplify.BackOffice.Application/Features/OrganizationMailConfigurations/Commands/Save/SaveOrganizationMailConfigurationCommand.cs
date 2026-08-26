using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Constants;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Rules;
using Symplify.BackOffice.Application.Features.Organizations.Commands;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.Save;

public sealed class SaveOrganizationMailConfigurationCommand :
    IRequest<SaveOrganizationMailConfigurationResponse>,
    ISecuredRequest,
    ICacheRemoverRequest
{
    public Guid OrganizationId { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyToEmail { get; set; }
    public string? ReplyToName { get; set; }
    public OrganizationLogoInputDto? MailLogo { get; set; }
    public bool RemoveMailLogo { get; set; }
    public bool IsActive { get; set; } = true;

    public string[] Roles =>
    [
        OrganizationsOperationClaims.Admin,
        OrganizationsOperationClaims.Write,
        OrganizationsOperationClaims.Update
    ];

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => $"OrganizationMailConfiguration({OrganizationId})";

    public sealed class Handler : IRequestHandler<SaveOrganizationMailConfigurationCommand, SaveOrganizationMailConfigurationResponse>
    {
        private const long MaxMailLogoSizeInBytes = 300 * 1024;

        private static readonly HashSet<string> AllowedMailLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg"
        };

        private readonly IOrganizationMailConfigurationRepository _repository;
        private readonly IMailCredentialProtector _credentialProtector;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly OrganizationMailConfigurationBusinessRules _rules;

        public Handler(
            IOrganizationMailConfigurationRepository repository,
            IMailCredentialProtector credentialProtector,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            OrganizationMailConfigurationBusinessRules rules)
        {
            _repository = repository;
            _credentialProtector = credentialProtector;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _rules = rules;
        }

        public async Task<SaveOrganizationMailConfigurationResponse> Handle(
            SaveOrganizationMailConfigurationCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.OrganizationShouldExistAsync(request.OrganizationId, cancellationToken);

            OrganizationMailConfiguration? entity = await _repository.GetAsync(
                predicate: item => item.OrganizationId == request.OrganizationId,
                cancellationToken: cancellationToken);

            bool created = entity is null;
            if (entity is null)
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                    throw new BusinessException(OrganizationMailConfigurationsMessages.PasswordRequired);

                entity = new OrganizationMailConfiguration
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = request.OrganizationId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "OrganizationMailConfiguration"
                };
            }

            OrganizationMailConfiguration configuration = entity;
            string? previousLogoBucketName = configuration.MailLogoBucketName;
            string? previousLogoObjectName = configuration.MailLogoObjectName;
            string? uploadedLogoBucketName = null;
            string? uploadedLogoObjectName = null;

            try
            {
                if (request.MailLogo is not null)
                {
                    var uploadedLogo = await UploadMailLogoAsync(
                        request.OrganizationId,
                        request.MailLogo,
                        cancellationToken);

                    uploadedLogoBucketName = uploadedLogo.BucketName;
                    uploadedLogoObjectName = uploadedLogo.ObjectName;
                    configuration.MailLogoBucketName = uploadedLogo.BucketName;
                    configuration.MailLogoObjectName = uploadedLogo.ObjectName;
                    configuration.MailLogoContentType = uploadedLogo.ContentType;
                    configuration.MailLogoFileName = uploadedLogo.FileName;
                }
                else if (request.RemoveMailLogo)
                {
                    configuration.MailLogoBucketName = null;
                    configuration.MailLogoObjectName = null;
                    configuration.MailLogoContentType = null;
                    configuration.MailLogoFileName = null;
                }

                configuration.Host = request.Host.Trim();
                configuration.Port = request.Port;
                configuration.EnableSsl = request.EnableSsl;
                configuration.Username = request.Username.Trim();
                configuration.FromEmail = request.FromEmail.Trim();
                configuration.FromName = request.FromName.Trim();
                configuration.ReplyToEmail = Normalize(request.ReplyToEmail);
                configuration.ReplyToName = Normalize(request.ReplyToName);
                configuration.IsActive = request.IsActive;
                configuration.DeletedDate = null;
                configuration.DeletedBy = null;
                configuration.UpdatedDate = DateTime.UtcNow;
                configuration.UpdatedBy = "OrganizationMailConfiguration";

                if (!string.IsNullOrWhiteSpace(request.Password))
                    configuration.PasswordCipherText = _credentialProtector.Protect(request.Password);

                if (created)
                    await _repository.AddAsync(configuration);
                else
                    await _repository.UpdateAsync(configuration);

                bool logoChanged = !string.IsNullOrWhiteSpace(previousLogoObjectName) &&
                                   (!string.Equals(previousLogoBucketName, configuration.MailLogoBucketName, StringComparison.Ordinal) ||
                                    !string.Equals(previousLogoObjectName, configuration.MailLogoObjectName, StringComparison.Ordinal));

                if (logoChanged)
                {
                    await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                        _objectStorageService,
                        previousLogoBucketName,
                        previousLogoObjectName,
                        cancellationToken);
                }

                return new SaveOrganizationMailConfigurationResponse
                {
                    Id = configuration.Id,
                    OrganizationId = configuration.OrganizationId,
                    Created = created
                };
            }
            catch
            {
                await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                    _objectStorageService,
                    uploadedLogoBucketName,
                    uploadedLogoObjectName,
                    cancellationToken);

                throw;
            }
        }

        private async Task<(string BucketName, string ObjectName, string ContentType, string FileName)> UploadMailLogoAsync(
            Guid organizationId,
            OrganizationLogoInputDto logo,
            CancellationToken cancellationToken)
        {
            ValidateMailLogo(logo);

            string bucketName = GetCongressImagesBucketName();
            string extension = Path.GetExtension(logo.OriginalFileName).ToLowerInvariant();
            string contentType = extension == ".png" ? "image/png" : "image/jpeg";
            string fileName = BackOfficeObjectStorageHelper.BuildImageFileName("organization-mail-logo", logo.OriginalFileName);
            string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
                "backoffice",
                "organizations",
                organizationId.ToString("D"),
                "mail",
                "branding",
                fileName);

            if (logo.Content.CanSeek)
                logo.Content.Position = 0;

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = fileName,
                    ContentType = contentType,
                    Size = logo.Length,
                    Content = logo.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = "organization-mail",
                        ["organization-id"] = organizationId.ToString("D"),
                        ["asset-type"] = "mail-logo"
                    }
                },
                cancellationToken);

            return (
                bucketName,
                uploadResult.ObjectName,
                contentType,
                fileName);
        }

        private static void ValidateMailLogo(OrganizationLogoInputDto logo)
        {
            string extension = Path.GetExtension(logo.OriginalFileName);

            if (logo.Length <= 0 ||
                logo.Length > MaxMailLogoSizeInBytes ||
                string.IsNullOrWhiteSpace(extension) ||
                !AllowedMailLogoExtensions.Contains(extension))
            {
                throw new BusinessException(OrganizationMailConfigurationsMessages.InvalidMailLogo);
            }
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(OrganizationMailConfigurationsMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
