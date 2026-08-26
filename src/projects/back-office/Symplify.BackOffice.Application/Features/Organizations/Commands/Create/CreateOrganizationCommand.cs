using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Organizations.Commands;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Features.Organizations.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Organizations.Commands.Create;

public class CreateOrganizationCommand : IRequest<CreatedOrganizationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string ShortName { get; set; } = string.Empty;

    public string? WebsiteUrl { get; set; }
    public string? HostUrl { get; set; }
    public string? Description { get; set; }

    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactNote { get; set; }

    public string? LogoLightPath { get; set; }
    public string? LogoDarkPath { get; set; }
    public OrganizationLogoInputDto? LogoLight { get; set; }
    public OrganizationLogoInputDto? LogoDark { get; set; }
    public string? BrandColor { get; set; }
    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizations";
    public string[] Roles => new[] { OrganizationsOperationClaims.Admin, OrganizationsOperationClaims.Write, OrganizationsOperationClaims.Add };

    public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, CreatedOrganizationResponse>
    {
        private readonly IOrganizationRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IMapper _mapper;
        private readonly OrganizationBusinessRules _rules;

        public CreateOrganizationCommandHandler(
            IOrganizationRepository repository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IMapper mapper,
            OrganizationBusinessRules rules)
        {
            _repository = repository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedOrganizationResponse> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
        {
            string code = NormalizeCode(request.Code);
            string slug = NormalizeCode(request.Slug ?? request.Code);
            string shortName = NormalizeShortName(request.ShortName);
            string? websiteUrl = NormalizeNullable(request.WebsiteUrl);

            await _rules.OrganizationCodeShouldBeUniqueWhenCreating(code);
            await _rules.OrganizationSlugShouldBeUniqueWhenCreating(slug);

            Guid organizationId = Guid.NewGuid();
            List<string> uploadedObjectNames = new();

            try
            {
                string? logoLightPath = request.LogoLight is not null
                    ? await UploadLogoAsync(organizationId, "light", request.LogoLight, uploadedObjectNames, cancellationToken)
                    : NormalizeNullable(request.LogoLightPath);

                string? logoDarkPath = request.LogoDark is not null
                    ? await UploadLogoAsync(organizationId, "dark", request.LogoDark, uploadedObjectNames, cancellationToken)
                    : NormalizeNullable(request.LogoDarkPath);

                Organization entity = new()
                {
                    Id = organizationId,
                    Name = request.Name.Trim(),
                    Code = code,
                    Slug = slug,
                    ShortName = shortName,
                    WebsiteUrl = websiteUrl,
                    HostUrl = NormalizeNullable(request.HostUrl) ?? websiteUrl,
                    Description = NormalizeNullable(request.Description),
                    ContactName = NormalizeNullable(request.ContactName),
                    ContactTitle = NormalizeNullable(request.ContactTitle),
                    ContactEmail = NormalizeNullable(request.ContactEmail),
                    ContactPhone = NormalizeNullable(request.ContactPhone),
                    ContactNote = NormalizeNullable(request.ContactNote),
                    LogoLightPath = logoLightPath,
                    LogoDarkPath = logoDarkPath,
                    BrandColor = NormalizeBrandColor(request.BrandColor) ?? "#487FFF",
                    IsActive = request.IsActive,
                };

                Organization createdEntity = await _repository.AddAsync(entity);
                return _mapper.Map<CreatedOrganizationResponse>(createdEntity);
            }
            catch
            {
                await DeleteUploadedObjectsAsync(uploadedObjectNames, cancellationToken);
                throw;
            }
        }

        private async Task<string> UploadLogoAsync(
            Guid organizationId,
            string variant,
            OrganizationLogoInputDto logo,
            ICollection<string> uploadedObjectNames,
            CancellationToken cancellationToken)
        {
            BackOfficeObjectStorageHelper.ValidateImage(
                logo.OriginalFileName,
                logo.Length,
                isRequired: false,
                requiredMessage: OrganizationsMessages.InvalidLogo,
                invalidMessage: OrganizationsMessages.InvalidLogo);

            string bucketName = GetCongressImagesBucketName();
            string fileName = BackOfficeObjectStorageHelper.BuildImageFileName($"organization-logo-{variant}", logo.OriginalFileName);
            string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
                "backoffice",
                "organizations",
                organizationId.ToString("D"),
                "logos",
                variant,
                fileName);

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = fileName,
                    ContentType = BackOfficeObjectStorageHelper.NormalizeContentType(logo.ContentType),
                    Size = logo.Length,
                    Content = logo.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = "organizations",
                        ["organization-id"] = organizationId.ToString("D"),
                        ["logo-variant"] = variant
                    }
                },
                cancellationToken);

            uploadedObjectNames.Add(uploadResult.ObjectName);
            return uploadResult.ObjectName;
        }

        private async Task DeleteUploadedObjectsAsync(IEnumerable<string> objectNames, CancellationToken cancellationToken)
        {
            string bucketName = GetCongressImagesBucketName();

            foreach (string objectName in objectNames)
                await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(_objectStorageService, bucketName, objectName, cancellationToken);
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(OrganizationsMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private static string NormalizeCode(string value) => value.Trim().ToLowerInvariant();

        private static string NormalizeShortName(string value)
        {
            string normalized = value.Trim().ToUpperInvariant();

            while (normalized.Contains("--", StringComparison.Ordinal))
                normalized = normalized.Replace("--", "-", StringComparison.Ordinal);

            return normalized.Trim('-');
        }

        private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? NormalizeBrandColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string color = value.Trim();

            if (color.Length != 7 || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit))
                return null;

            return color.ToUpperInvariant();
        }
    }
}
