using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Congresses.Commands;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Features.Congresses.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Update;

public class UpdateCongressCommand : IRequest<UpdatedCongressResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public int? EditionNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CongressStatus Status { get; set; }
    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }
    public string? VenueName { get; set; }
    public string? LogoLightPath { get; set; }
    public string? LogoDarkPath { get; set; }
    public CongressLogoInputDto? LogoLight { get; set; }
    public CongressLogoInputDto? LogoDark { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public ICollection<UpdateCongressContactEmailInputDto> ContactEmails { get; set; }
        = new List<UpdateCongressContactEmailInputDto>();
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongresses";
    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Write, CongressesOperationClaims.Update };

    public class UpdateCongressCommandHandler : IRequestHandler<UpdateCongressCommand, UpdatedCongressResponse>
    {
        private static readonly string[] TranslationFieldNames =
        {
            "Title", "Subtitle", "WelcomeContent", "SeoTitle", "SeoDescription"
        };

        private readonly ICongressRepository _repository;
        private readonly ICongressTranslationRepository _translationRepository;
        private readonly ICongressContactEmailRepository _contactEmailRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressBusinessRules _rules;

        public UpdateCongressCommandHandler(
            ICongressRepository repository,
            ICongressTranslationRepository translationRepository,
            ICongressContactEmailRepository contactEmailRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _contactEmailRepository = contactEmailRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedCongressResponse> Handle(UpdateCongressCommand request, CancellationToken cancellationToken)
        {
            await _rules.OrganizationShouldExistAndBeActive(request.OrganizationId, cancellationToken);
            await _rules.DateRangeShouldBeValid(request.StartDate, request.EndDate);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await TranslationTitlesShouldBeValidAsync(request.Translations, cancellationToken);
            TranslationFieldLengthsShouldBeValid(request.Translations);

            List<UpdateCongressContactEmailInputDto> contactEmails =
                NormalizeContactEmails(request.ContactEmails);

            if (contactEmails.Count == 0)
                throw new BusinessException("En az bir geçerli kongre iletişim e-posta adresi girilmelidir.");

            if (contactEmails.Count(item => item.IsPrimary) > 1)
                throw new BusinessException("Yalnızca bir iletişim e-posta adresi ana adres olarak seçilebilir.");

            if (contactEmails.All(item => !item.IsPrimary))
                contactEmails[0].IsPrimary = true;

            Congress? entity = await _repository.GetAsync(predicate: congress => congress.Id == request.Id);
            await _rules.CongressShouldExistWhenSelected(entity);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            TranslationInputDto defaultTranslation = request.Translations.First(translation => translation.LanguageId == defaultLanguage.Id);
            string defaultTitle = NormalizeRequired(defaultTranslation.Fields.GetValueOrDefault("Title"));

            string? oldLogoLightPath = entity!.LogoLightPath;
            string? oldLogoDarkPath = entity.LogoDarkPath;
            List<string> uploadedObjectNames = new();

            try
            {
                string? logoLightPath = request.LogoLight is not null
                    ? await UploadLogoAsync(request.OrganizationId, entity.Id, "light", request.LogoLight, uploadedObjectNames, cancellationToken)
                    : Normalize(request.LogoLightPath) ?? entity.LogoLightPath;

                string? logoDarkPath = request.LogoDark is not null
                    ? await UploadLogoAsync(request.OrganizationId, entity.Id, "dark", request.LogoDark, uploadedObjectNames, cancellationToken)
                    : Normalize(request.LogoDarkPath) ?? entity.LogoDarkPath;

                entity.OrganizationId = request.OrganizationId;
                entity.Name = defaultTitle;
                entity.EditionNumber = request.EditionNumber;
                entity.StartDate = ToUtc(request.StartDate);
                entity.EndDate = ToUtc(request.EndDate);
                entity.Status = request.Status;
                entity.ContactName = Normalize(request.ContactName);
                entity.ContactTitle = Normalize(request.ContactTitle);
                entity.ContactEmail = contactEmails
                    .OrderByDescending(item => item.IsPrimary)
                    .ThenBy(item => item.Order)
                    .Select(item => item.Email)
                    .FirstOrDefault();
                entity.ContactPhone = Normalize(request.ContactPhone);
                entity.ContactAddress = Normalize(request.ContactAddress);
                entity.VenueName = Normalize(request.VenueName);
                entity.LogoLightPath = logoLightPath;
                entity.LogoDarkPath = logoDarkPath;
                entity.CountryId = request.CountryId;
                entity.CityId = null;
                entity.StateId = request.StateId;

                Congress updatedEntity = await _repository.UpdateAsync(entity);
                await UpsertContactEmailsAsync(request.Id, contactEmails, cancellationToken);
                await UpsertTranslationsAsync(request.Id, request.Translations, cancellationToken);

                await DeleteReplacedLogoAsync(oldLogoLightPath, entity.LogoLightPath, entity.Id, cancellationToken);
                await DeleteReplacedLogoAsync(oldLogoDarkPath, entity.LogoDarkPath, entity.Id, cancellationToken);

                return _mapper.Map<UpdatedCongressResponse>(updatedEntity);
            }
            catch
            {
                await DeleteUploadedObjectsAsync(uploadedObjectNames, cancellationToken);
                throw;
            }
        }

        private async Task<string> UploadLogoAsync(
            Guid organizationId,
            Guid congressId,
            string variant,
            CongressLogoInputDto logo,
            ICollection<string> uploadedObjectNames,
            CancellationToken cancellationToken)
        {
            BackOfficeObjectStorageHelper.ValidateImage(
                logo.OriginalFileName,
                logo.Length,
                isRequired: false,
                requiredMessage: CongressesMessages.InvalidLogo,
                invalidMessage: CongressesMessages.InvalidLogo);

            string bucketName = GetCongressImagesBucketName();
            string fileName = BackOfficeObjectStorageHelper.BuildImageFileName($"congress-logo-{variant}", logo.OriginalFileName);
            string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
                "backoffice",
                "organizations",
                organizationId.ToString("D"),
                "congresses",
                congressId.ToString("D"),
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
                        ["module"] = "congresses",
                        ["organization-id"] = organizationId.ToString("D"),
                        ["congress-id"] = congressId.ToString("D"),
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

        private async Task DeleteReplacedLogoAsync(string? oldPath, string? newPath, Guid congressId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(oldPath) || string.Equals(oldPath, newPath, StringComparison.Ordinal))
                return;

            if (!IsCongressOwnedLogoObject(oldPath, congressId))
                return;

            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(_objectStorageService, GetCongressImagesBucketName(), oldPath, cancellationToken);
        }

        private static bool IsCongressOwnedLogoObject(string objectName, Guid congressId)
        {
            return objectName.Contains($"/congresses/{congressId:D}/logos/", StringComparison.OrdinalIgnoreCase);
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressesMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private static List<UpdateCongressContactEmailInputDto> NormalizeContactEmails(
            IEnumerable<UpdateCongressContactEmailInputDto>? contactEmails)
        {
            return (contactEmails ?? Array.Empty<UpdateCongressContactEmailInputDto>())
                .Where(item => !string.IsNullOrWhiteSpace(item.Email))
                .Select((item, index) => new UpdateCongressContactEmailInputDto
                {
                    Label = Normalize(item.Label),
                    Email = item.Email!.Trim().ToLowerInvariant(),
                    IsPrimary = item.IsPrimary,
                    IsVisibleOnPortal = item.IsVisibleOnPortal,
                    ReceivesContactMessages = item.ReceivesContactMessages,
                    Order = index
                })
                .GroupBy(item => item.Email, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private async Task UpsertContactEmailsAsync(
            Guid congressId,
            IReadOnlyCollection<UpdateCongressContactEmailInputDto> inputs,
            CancellationToken cancellationToken)
        {
            List<CongressContactEmail> existing = _contactEmailRepository.Query()
                .Where(item => item.CongressId == congressId)
                .OrderBy(item => item.Order)
                .ToList();

            // Partial unique primary index nedeniyle önce mevcut primary işaretini güvenli biçimde indir.
            foreach (CongressContactEmail item in existing.Where(item => item.IsPrimary))
            {
                item.IsPrimary = false;
                await _contactEmailRepository.UpdateAsync(item);
            }

            Dictionary<string, CongressContactEmail> existingByEmail = existing
                .GroupBy(item => item.Email, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            HashSet<Guid> retainedIds = new();

            foreach (UpdateCongressContactEmailInputDto input in inputs.OrderBy(item => item.Order))
            {
                if (existingByEmail.TryGetValue(input.Email!, out CongressContactEmail? entity))
                {
                    entity.Label = Normalize(input.Label);
                    entity.IsPrimary = input.IsPrimary;
                    entity.IsVisibleOnPortal = input.IsVisibleOnPortal;
                    entity.ReceivesContactMessages = input.ReceivesContactMessages;
                    entity.Order = input.Order;

                    await _contactEmailRepository.UpdateAsync(entity);
                    retainedIds.Add(entity.Id);
                    continue;
                }

                CongressContactEmail newEntity = new()
                {
                    Id = Guid.NewGuid(),
                    CongressId = congressId,
                    Email = input.Email!,
                    Label = Normalize(input.Label),
                    IsPrimary = input.IsPrimary,
                    IsVisibleOnPortal = input.IsVisibleOnPortal,
                    ReceivesContactMessages = input.ReceivesContactMessages,
                    Order = input.Order
                };

                await _contactEmailRepository.AddAsync(newEntity);
                retainedIds.Add(newEntity.Id);
            }

            foreach (CongressContactEmail item in existing.Where(item => !retainedIds.Contains(item.Id)))
                await _contactEmailRepository.DeleteAsync(item);
        }

        private static void TranslationFieldLengthsShouldBeValid(IEnumerable<TranslationInputDto> translations)
        {
            foreach (TranslationInputDto input in translations)
            {
                FieldLengthShouldBeValid(input, "Title", 300);
                FieldLengthShouldBeValid(input, "Subtitle", 300);
                FieldLengthShouldBeValid(input, "WelcomeContent", 20000);
                FieldLengthShouldBeValid(input, "SeoTitle", 300);
                FieldLengthShouldBeValid(input, "SeoDescription", 500);
            }
        }

        private static void FieldLengthShouldBeValid(TranslationInputDto input, string fieldName, int maxLength)
        {
            string? value = GetFieldValue(input, fieldName);

            if (value is not null && value.Length > maxLength)
                throw new BusinessException(CongressesMessages.TranslationFieldMaxLengthExceeded);
        }

        private async Task TranslationTitlesShouldBeValidAsync(IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            foreach (TranslationInputDto input in translations)
            {
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                string? title = GetFieldValue(input, "Title");

                if ((isDefaultLanguage || hasAnyValue) && string.IsNullOrWhiteSpace(title))
                    throw new BusinessException(CongressesMessages.TranslationTitleRequired);
            }
        }

        private async Task UpsertTranslationsAsync(Guid congressId, IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<CongressTranslation> existingTranslations = _translationRepository.Query().Where(translation => translation.CongressId == congressId).ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                string? title = GetFieldValue(input, "Title");

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                if (string.IsNullOrWhiteSpace(title))
                    throw new BusinessException(CongressesMessages.TranslationTitleRequired);

                CongressTranslation? existingTranslation = existingTranslations.FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    CongressTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressId", congressId);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }

                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
        }

        private static string? GetFieldValue(TranslationInputDto input, string fieldName)
        {
            return input.Fields.TryGetValue(fieldName, out string? value) ? value : null;
        }

        private static string NormalizeRequired(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static DateTime? ToUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }
    }
}
