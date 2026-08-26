using AutoMapper;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Congresses.Cloning;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Features.Congresses.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Create;

public class CreateCongressCommand : IRequest<CreatedCongressResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid OrganizationId { get; set; }
    public int? EditionNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CongressStatus Status { get; set; } = CongressStatus.Draft;
    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }
    public string? VenueName { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }

    public ICollection<CreateCongressContactEmailInputDto> ContactEmails { get; set; }
        = new List<CreateCongressContactEmailInputDto>();

    public Guid? CopyFromCongressId { get; set; }

    public bool ShiftRelativeDates { get; set; } = true;

    public ICollection<CongressCloneModule> CloneModules { get; set; }
        = new List<CongressCloneModule>();

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongresses";
    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Write, CongressesOperationClaims.Add };

    public class CreateCongressCommandHandler : IRequestHandler<CreateCongressCommand, CreatedCongressResponse>
    {
        private static readonly string[] TranslationFieldNames =
        {
            "Title", "Subtitle", "WelcomeContent", "SeoTitle", "SeoDescription"
        };

        private readonly ICongressRepository _repository;
        private readonly ICongressTranslationRepository _translationRepository;
        private readonly ICongressContactEmailRepository _contactEmailRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IWorkflowTemplateRepository _workflowTemplateRepository;
        private readonly IWorkflowTemplateCopyService _workflowTemplateCopyService;
        private readonly ICongressCloneService _congressCloneService;
        private readonly IMapper _mapper;
        private readonly CongressBusinessRules _rules;

        public CreateCongressCommandHandler(
            ICongressRepository repository,
            ICongressTranslationRepository translationRepository,
            ICongressContactEmailRepository contactEmailRepository,
            IApplicationLanguageProvider languageProvider,
            IWorkflowTemplateRepository workflowTemplateRepository,
            IWorkflowTemplateCopyService workflowTemplateCopyService,
            ICongressCloneService congressCloneService,
            IMapper mapper,
            CongressBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _contactEmailRepository = contactEmailRepository;
            _languageProvider = languageProvider;
            _workflowTemplateRepository = workflowTemplateRepository;
            _workflowTemplateCopyService = workflowTemplateCopyService;
            _congressCloneService = congressCloneService;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedCongressResponse> Handle(CreateCongressCommand request, CancellationToken cancellationToken)
        {
            Organization organization = await _rules.OrganizationShouldExistAndBeActive(request.OrganizationId, cancellationToken);
            await _rules.DateRangeShouldBeValid(request.StartDate, request.EndDate);

            bool hasCloneSource =
                request.CopyFromCongressId.HasValue &&
                request.CopyFromCongressId.Value != Guid.Empty;

            HashSet<CongressCloneModule> selectedModules = request.CloneModules
                .Distinct()
                .ToHashSet();

            bool hasCloneModules = selectedModules.Count > 0;

            if (hasCloneSource != hasCloneModules)
            {
                throw new BusinessException(
                    "Kongre kopyalama için kaynak kongre ve en az bir alan birlikte seçilmelidir.");
            }

            bool cloneRequested = hasCloneSource && hasCloneModules;
            bool copiesGeneralInformation =
                cloneRequested &&
                selectedModules.Contains(CongressCloneModule.GeneralInformation);

            List<CreateCongressContactEmailInputDto> contactEmails =
                NormalizeContactEmails(request.ContactEmails);

            if (!copiesGeneralInformation && contactEmails.Count == 0)
            {
                throw new BusinessException(
                    "En az bir geçerli kongre iletişim e-posta adresi girilmelidir.");
            }

            if (contactEmails.Count(item => item.IsPrimary) > 1)
            {
                throw new BusinessException(
                    "Yalnızca bir iletişim e-posta adresi ana adres olarak seçilebilir.");
            }

            if (contactEmails.Count > 0 && contactEmails.All(item => !item.IsPrimary))
            {
                contactEmails[0].IsPrimary = true;
            }

            if (copiesGeneralInformation)
            {
                await DefaultTranslationTitleShouldExistAsync(
                    request.Translations,
                    cancellationToken);
            }
            else
            {
                await _rules.DefaultTranslationShouldExist(
                    request.Translations,
                    cancellationToken);
            }

            await TranslationTitlesShouldBeValidAsync(request.Translations, cancellationToken);
            TranslationFieldLengthsShouldBeValid(request.Translations);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            TranslationInputDto defaultTranslation = request.Translations.First(translation => translation.LanguageId == defaultLanguage.Id);
            string defaultTitle = NormalizeRequired(defaultTranslation.Fields.GetValueOrDefault("Title"));

            string code = await GenerateUniqueCodeAsync(organization, request.StartDate, cancellationToken);
            string slug = await GenerateUniqueSlugAsync(request.OrganizationId, organization, request.EditionNumber, defaultTitle, request.StartDate);

            await _rules.CodeShouldBeUnique(request.OrganizationId, code);
            await _rules.SlugShouldBeUnique(request.OrganizationId, slug);

            string? primaryContactEmail = contactEmails
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => item.Order)
                .Select(item => item.Email)
                .FirstOrDefault();

            Congress entity = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Code = code,
                Name = defaultTitle,
                Slug = slug,
                EditionNumber = request.EditionNumber,
                StartDate = ToUtc(request.StartDate),
                EndDate = ToUtc(request.EndDate),
                Status = request.Status,
                ContactName = Normalize(request.ContactName),
                ContactTitle = Normalize(request.ContactTitle),
                ContactEmail = Normalize(primaryContactEmail),
                ContactPhone = Normalize(request.ContactPhone),
                ContactAddress = Normalize(request.ContactAddress),
                VenueName = Normalize(request.VenueName),
                LogoLightPath = Normalize(organization.LogoLightPath),
                LogoDarkPath = Normalize(organization.LogoDarkPath),
                CountryId = request.CountryId,
                StateId = request.StateId
            };

            Congress createdEntity = await _repository.AddAsync(entity);

            try
            {
                await CreateTranslationsAsync(
                    createdEntity.Id,
                    request.Translations,
                    cancellationToken);

                await CreateContactEmailsAsync(
                    createdEntity.Id,
                    contactEmails,
                    cancellationToken);

                // Kaynaktan workflow kopyalanmayacaksa mevcut default workflow davranışı korunur.
                if (!cloneRequested ||
                    !selectedModules.Contains(CongressCloneModule.Workflow))
                {
                    await ApplyDefaultWorkflowTemplateIfExistsAsync(
                        createdEntity.Id,
                        cancellationToken);
                }

                if (cloneRequested)
                {
                    await _congressCloneService.CloneAsync(
                        new CongressCloneRequest
                        {
                            SourceCongressId = request.CopyFromCongressId!.Value,
                            TargetCongressId = createdEntity.Id,
                            ShiftRelativeDates = request.ShiftRelativeDates,
                            Modules = selectedModules
                        },
                        cancellationToken);
                }
            }
            catch
            {
                try
                {
                    await _congressCloneService.DeleteCreatedCongressAsync(
                        createdEntity.Id,
                        cancellationToken);
                }
                catch
                {
                    // Asıl oluşturma/kopyalama hatasını maskelememek için cleanup hatası yutulur.
                }

                throw;
            }

            return _mapper.Map<CreatedCongressResponse>(createdEntity);
        }


        private static List<CreateCongressContactEmailInputDto> NormalizeContactEmails(
            IEnumerable<CreateCongressContactEmailInputDto>? source)
        {
            return (source ?? Array.Empty<CreateCongressContactEmailInputDto>())
                .Where(item => !string.IsNullOrWhiteSpace(item.Email))
                .Select((item, index) => new CreateCongressContactEmailInputDto
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

        private async Task CreateContactEmailsAsync(
            Guid congressId,
            IReadOnlyCollection<CreateCongressContactEmailInputDto> contactEmails,
            CancellationToken cancellationToken)
        {
            foreach (CreateCongressContactEmailInputDto input in contactEmails
                         .OrderBy(item => item.Order))
            {
                CongressContactEmail entity = new()
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

                await _contactEmailRepository.AddAsync(entity);
            }
        }

        private async Task ApplyDefaultWorkflowTemplateIfExistsAsync(Guid congressId, CancellationToken cancellationToken)
        {
            WorkflowTemplate? defaultTemplate = _workflowTemplateRepository.Query()
                .Where(template => template.IsActive && template.IsDefault)
                .OrderBy(template => template.Code)
                .ThenBy(template => template.Id)
                .FirstOrDefault();

            if (defaultTemplate is null)
                return;

            await _workflowTemplateCopyService.ApplyTemplateToCongressAsync(
                congressId,
                defaultTemplate.Id,
                replaceExistingTransitions: true,
                cancellationToken);
        }

        private async Task DefaultTranslationTitleShouldExistAsync(
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage =
                await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            TranslationInputDto? defaultTranslation = translations
                .FirstOrDefault(translation =>
                    translation.LanguageId == defaultLanguage.Id);

            if (defaultTranslation is null ||
                !LocalizedEntityRuntimeHelper.HasRequiredField(
                    defaultTranslation.Fields,
                    "Title"))
            {
                throw new BusinessException(
                    CongressesMessages.TranslationTitleRequired);
            }
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

        private async Task CreateTranslationsAsync(Guid congressId, IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

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

                CongressTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressId", congressId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }

        private static string? GetFieldValue(TranslationInputDto input, string fieldName)
        {
            return input.Fields.TryGetValue(fieldName, out string? value) ? value : null;
        }

        private async Task<string> GenerateUniqueCodeAsync(Organization organization, DateTime? startDate, CancellationToken cancellationToken)
        {
            string prefix = NormalizeCodePrefix(organization.ShortName);
            int year = startDate?.Year ?? DateTime.UtcNow.Year;
            int sequence = _repository.Query().Count(congress => congress.OrganizationId == organization.Id && congress.Code.StartsWith($"{prefix}-{year}-")) + 1;

            string code;
            do
            {
                code = $"{prefix}-{year}-{sequence:000}";
                sequence++;
            }
            while (_repository.Query().Any(congress => congress.OrganizationId == organization.Id && congress.Code == code));

            return await Task.FromResult(code);
        }

        private async Task<string> GenerateUniqueSlugAsync(Guid organizationId, Organization organization, int? editionNumber, string title, DateTime? startDate)
        {
            string prefix = NormalizeCodePrefix(organization.ShortName);
            int year = startDate?.Year ?? DateTime.UtcNow.Year;
            string baseText = editionNumber.HasValue
                ? $"{prefix}-{editionNumber.Value}-kongre-{year}"
                : $"{prefix}-{year}-{title}";
            string baseSlug = NormalizeSlug(baseText);
            string slug = baseSlug;
            int suffix = 2;

            while (_repository.Query().Any(congress => congress.OrganizationId == organizationId && congress.Slug == slug))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            return await Task.FromResult(slug);
        }

        private static string NormalizeCodePrefix(string value)
        {
            string normalized = new string(value.Trim().ToUpperInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
            while (normalized.Contains("--", StringComparison.Ordinal)) normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
            return normalized.Trim('-');
        }

        private static string NormalizeSlug(string value)
        {
            string normalized = new string(value.Trim().ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
            while (normalized.Contains("--", StringComparison.Ordinal)) normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
            return normalized.Trim('-');
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
