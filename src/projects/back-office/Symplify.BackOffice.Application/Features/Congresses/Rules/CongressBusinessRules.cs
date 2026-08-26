using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Congresses.Rules;

public class CongressBusinessRules : BaseBusinessRules
{
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly ICongressRepository _congressRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public CongressBusinessRules(
        IApplicationLanguageProvider applicationLanguageProvider,
        ICongressRepository congressRepository,
        IOrganizationRepository organizationRepository)
    {
        _applicationLanguageProvider = applicationLanguageProvider;
        _congressRepository = congressRepository;
        _organizationRepository = organizationRepository;
    }

    public Task CongressShouldExistWhenSelected(Congress? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressesMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public async Task<Organization> OrganizationShouldExistAndBeActive(Guid organizationId, CancellationToken cancellationToken)
    {
        Organization? organization = await _organizationRepository.GetAsync(predicate: organization => organization.Id == organizationId);

        if (organization is null)
            throw new BusinessException(CongressesMessages.OrganizationNotFound);

        if (!organization.IsActive)
            throw new BusinessException(CongressesMessages.OrganizationInactive);

        if (string.IsNullOrWhiteSpace(organization.ShortName))
            throw new BusinessException(CongressesMessages.OrganizationShortNameRequired);

        return organization;
    }

    public Task DateRangeShouldBeValid(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
            throw new BusinessException(CongressesMessages.DateRangeInvalid);

        return Task.CompletedTask;
    }

    public Task SlugShouldBeUnique(Guid organizationId, string? slug, Guid? excludedCongressId = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.CompletedTask;

        string normalizedSlug = slug.Trim().ToLowerInvariant();

        bool exists = _congressRepository.Query()
            .Any(congress =>
                congress.OrganizationId == organizationId &&
                congress.Slug != null &&
                congress.Slug.ToLower() == normalizedSlug &&
                (!excludedCongressId.HasValue || congress.Id != excludedCongressId.Value));

        if (exists)
            throw new BusinessException(CongressesMessages.SlugAlreadyExists);

        return Task.CompletedTask;
    }

    public Task CodeShouldBeUnique(Guid organizationId, string code, Guid? excludedCongressId = null)
    {
        string normalizedCode = code.Trim().ToUpperInvariant();

        bool exists = _congressRepository.Query()
            .Any(congress =>
                congress.OrganizationId == organizationId &&
                congress.Code.ToUpper() == normalizedCode &&
                (!excludedCongressId.HasValue || congress.Id != excludedCongressId.Value));

        if (exists)
            throw new BusinessException(CongressesMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public async Task DefaultTranslationShouldExist(IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
    {
        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);
        TranslationInputDto? defaultTranslation = translations.FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

        if (defaultTranslation is null ||
            !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "Title") ||
            !LocalizedEntityRuntimeHelper.HasRequiredField(defaultTranslation.Fields, "WelcomeContent"))
        {
            throw new BusinessException(CongressesMessages.DefaultTranslationRequired);
        }
    }

    public Task PublishDateRangeShouldBeValid(DateTime? publishStartDate, DateTime? publishEndDate)
    {
        if (publishStartDate.HasValue && publishEndDate.HasValue && publishEndDate.Value < publishStartDate.Value)
            throw new BusinessException(CongressesMessages.PublishDateRangeInvalid);

        return Task.CompletedTask;
    }
}
