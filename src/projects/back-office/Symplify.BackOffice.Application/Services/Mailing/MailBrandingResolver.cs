using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Domain.Submission;
using OrganizationEntity = Symplify.BackOffice.Domain.Organization.Organization;

namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class MailBrandingResolver : IMailBrandingResolver
{
    private readonly ICongressRepository _congressRepository;
    private readonly ICongressTranslationRepository _congressTranslationRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMailConfigurationRepository _mailConfigurationRepository;
    private readonly ILanguageRepository _languageRepository;
    private readonly MailTemplateOptions _options;

    public MailBrandingResolver(
        ICongressRepository congressRepository,
        ICongressTranslationRepository congressTranslationRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationMailConfigurationRepository mailConfigurationRepository,
        ILanguageRepository languageRepository,
        IOptions<MailTemplateOptions> options)
    {
        _congressRepository = congressRepository;
        _congressTranslationRepository = congressTranslationRepository;
        _organizationRepository = organizationRepository;
        _mailConfigurationRepository = mailConfigurationRepository;
        _languageRepository = languageRepository;
        _options = options.Value;
    }

    public Task<MailBrandingModel> ResolveForSubmissionAsync(
        Submission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        return ResolveForCongressAsync(
            submission.CongressId,
            submission.LanguageId,
            submission.Language?.Culture,
            cancellationToken);
    }

    public async Task<MailBrandingModel> ResolveForCongressAsync(
        Guid congressId,
        Guid? languageId = null,
        string? culture = null,
        CancellationToken cancellationToken = default)
    {
        Congress? congress = await _congressRepository.GetAsync(
            predicate: item => item.Id == congressId && item.DeletedDate == null,
            cancellationToken: cancellationToken);

        if (congress is null)
            return ResolveDefault();

        OrganizationEntity? organization = await _organizationRepository.GetAsync(
            predicate: item => item.Id == congress.OrganizationId && item.DeletedDate == null,
            cancellationToken: cancellationToken);

        CongressTranslation? translation = await ResolveTranslationAsync(
            congress.Id,
            languageId,
            culture,
            cancellationToken);

        string contextTitle = FirstNonEmpty(
            translation?.Title,
            congress.Name,
            congress.Code,
            organization?.Name,
            _options.BrandName,
            "Symplify");

        OrganizationMailConfiguration? mailConfiguration = await GetMailConfigurationAsync(
            congress.OrganizationId,
            cancellationToken);

        return BuildModel(
            brandName: contextTitle,
            contextTitle: contextTitle,
            logoAltText: FirstNonEmpty(organization?.ShortName, organization?.Name, contextTitle),
            mailConfiguration: mailConfiguration);
    }

    public async Task<MailBrandingModel> ResolveForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
            return ResolveDefault();

        OrganizationEntity? organization = await _organizationRepository.GetAsync(
            predicate: item => item.Id == organizationId && item.DeletedDate == null,
            cancellationToken: cancellationToken);

        if (organization is null)
            return ResolveDefault();

        OrganizationMailConfiguration? mailConfiguration = await GetMailConfigurationAsync(
            organizationId,
            cancellationToken);

        string brandName = FirstNonEmpty(
            organization.ShortName,
            organization.Name,
            _options.BrandName,
            "Symplify");

        string contextTitle = FirstNonEmpty(
            organization.Name,
            organization.ShortName,
            brandName);

        return BuildModel(
            brandName,
            contextTitle,
            FirstNonEmpty(organization.ShortName, organization.Name, brandName),
            mailConfiguration);
    }

    public MailBrandingModel ResolveDefault()
    {
        string brandName = ResolveBrandName();

        return new MailBrandingModel
        {
            BrandName = brandName,
            ContextTitle = brandName,
            LogoContentId = null,
            LogoAltText = brandName
        };
    }

    private async Task<CongressTranslation?> ResolveTranslationAsync(
        Guid congressId,
        Guid? languageId,
        string? culture,
        CancellationToken cancellationToken)
    {
        CongressTranslation? translation = null;

        if (languageId.HasValue && languageId.Value != Guid.Empty)
        {
            translation = await _congressTranslationRepository.GetAsync(
                predicate: item =>
                    item.CongressId == congressId &&
                    item.LanguageId == languageId.Value &&
                    item.DeletedDate == null,
                cancellationToken: cancellationToken);
        }

        if (translation is null && !string.IsNullOrWhiteSpace(culture))
        {
            string normalizedCulture = culture.Trim();
            Language? requestedLanguage = await _languageRepository.GetAsync(
                predicate: item =>
                    item.IsActive &&
                    item.DeletedDate == null &&
                    item.Culture == normalizedCulture,
                cancellationToken: cancellationToken);

            if (requestedLanguage is not null)
            {
                translation = await _congressTranslationRepository.GetAsync(
                    predicate: item =>
                        item.CongressId == congressId &&
                        item.LanguageId == requestedLanguage.Id &&
                        item.DeletedDate == null,
                    cancellationToken: cancellationToken);
            }
        }

        if (translation is not null)
            return translation;

        Language? defaultLanguage = await _languageRepository.GetAsync(
            predicate: item => item.IsDefault && item.IsActive && item.DeletedDate == null,
            cancellationToken: cancellationToken);

        if (defaultLanguage is null)
            return null;

        return await _congressTranslationRepository.GetAsync(
            predicate: item =>
                item.CongressId == congressId &&
                item.LanguageId == defaultLanguage.Id &&
                item.DeletedDate == null,
            cancellationToken: cancellationToken);
    }

    private Task<OrganizationMailConfiguration?> GetMailConfigurationAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return _mailConfigurationRepository.GetAsync(
            predicate: item =>
                item.OrganizationId == organizationId &&
                item.IsActive &&
                item.DeletedDate == null,
            cancellationToken: cancellationToken);
    }

    private static MailBrandingModel BuildModel(
        string brandName,
        string contextTitle,
        string logoAltText,
        OrganizationMailConfiguration? mailConfiguration)
    {
        bool hasPrivateLogo = mailConfiguration is not null &&
                              !string.IsNullOrWhiteSpace(mailConfiguration.MailLogoBucketName) &&
                              !string.IsNullOrWhiteSpace(mailConfiguration.MailLogoObjectName);

        return new MailBrandingModel
        {
            BrandName = string.IsNullOrWhiteSpace(brandName) ? "Symplify" : brandName.Trim(),
            ContextTitle = string.IsNullOrWhiteSpace(contextTitle) ? null : contextTitle.Trim(),
            LogoContentId = hasPrivateLogo ? MailBrandingModel.OrganizationLogoContentId : null,
            LogoAltText = string.IsNullOrWhiteSpace(logoAltText) ? brandName : logoAltText.Trim()
        };
    }

    private string ResolveBrandName()
    {
        return string.IsNullOrWhiteSpace(_options.BrandName) ? "Symplify" : _options.BrandName.Trim();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }
}
