using MediatR;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Reference.Translations;

namespace Symplify.BackOffice.Application.Features.Auth.Queries.GetRegisterOptions;

public sealed class GetRegisterOptionsQuery : IRequest<GetRegisterOptionsResponse>
{
    public string? Culture { get; set; }

    public sealed class GetRegisterOptionsQueryHandler : IRequestHandler<GetRegisterOptionsQuery, GetRegisterOptionsResponse>
    {
        private readonly ITitleRepository _titleRepository;
        private readonly ITitleTranslationRepository _titleTranslationRepository;
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressTranslationRepository _congressTranslationRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly ICountryTranslationRepository _countryTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetRegisterOptionsQueryHandler(
            ITitleRepository titleRepository,
            ITitleTranslationRepository titleTranslationRepository,
            ICongressRepository congressRepository,
            ICongressTranslationRepository congressTranslationRepository,
            ICountryRepository countryRepository,
            ICountryTranslationRepository countryTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _titleRepository = titleRepository;
            _titleTranslationRepository = titleTranslationRepository;
            _congressRepository = congressRepository;
            _congressTranslationRepository = congressTranslationRepository;
            _countryRepository = countryRepository;
            _countryTranslationRepository = countryTranslationRepository;
            _languageProvider = languageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetRegisterOptionsResponse> Handle(
            GetRegisterOptionsQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.Culture, defaultLanguage, cancellationToken);

            return new GetRegisterOptionsResponse
            {
                Titles = BuildTitleOptions(requestedLanguage.Id, defaultLanguage.Id),
                Congresses = BuildCongressOptions(requestedLanguage.Id, defaultLanguage.Id),
                Countries = BuildCountryOptions(requestedLanguage.Id, defaultLanguage.Id)
            };
        }

        private List<AuthSelectOptionDto> BuildTitleOptions(Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<Title> titles = _titleRepository.Query()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Id)
                .ToList();

            List<TitleTranslation> translations = _titleTranslationRepository.Query().ToList();

            return titles.Select(title =>
            {
                List<TitleTranslation> rootTranslations = translations
                    .Where(translation => translation.TitleId == title.Id)
                    .ToList();

                TitleTranslation? translation = _fallbackResolver.Resolve(rootTranslations, requestedLanguageId, defaultLanguageId);

                return new AuthSelectOptionDto
                {
                    Value = title.Id.ToString("D"),
                    Text = translation?.Name ?? title.Code ?? title.Id.ToString("D")
                };
            }).ToList();
        }

        private List<AuthSelectOptionDto> BuildCongressOptions(Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<Congress> congresses = _congressRepository.Query()
                .OrderByDescending(item => item.StartDate)
                .ThenBy(item => item.Name)
                .ToList();

            List<CongressTranslation> translations = _congressTranslationRepository.Query().ToList();

            return congresses.Select(congress =>
            {
                List<CongressTranslation> rootTranslations = translations
                    .Where(translation => translation.CongressId == congress.Id)
                    .ToList();

                CongressTranslation? translation = _fallbackResolver.Resolve(rootTranslations, requestedLanguageId, defaultLanguageId);

                string title = !string.IsNullOrWhiteSpace(translation?.Title)
                    ? translation.Title
                    : !string.IsNullOrWhiteSpace(congress.Name)
                        ? congress.Name
                        : congress.Code;

                return new AuthSelectOptionDto
                {
                    Value = congress.Id.ToString("D"),
                    Text = title
                };
            }).ToList();
        }

        private List<AuthSelectOptionDto> BuildCountryOptions(Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<Symplify.BackOffice.Domain.Reference.Country> countries = _countryRepository.Query()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Code)
                .ToList();

            List<CountryTranslation> translations = _countryTranslationRepository.Query().ToList();

            return countries.Select(country =>
            {
                List<CountryTranslation> rootTranslations = translations
                    .Where(translation => translation.CountryId == country.Id)
                    .ToList();

                CountryTranslation? translation = _fallbackResolver.Resolve(rootTranslations, requestedLanguageId, defaultLanguageId);

                return new AuthSelectOptionDto
                {
                    Value = country.Id.ToString("D"),
                    Text = translation?.Name ?? country.Code ?? country.Id.ToString("D")
                };
            }).ToList();
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(
            string? culture,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(culture))
                return defaultLanguage;

            ApplicationLanguageDto? language = await _languageProvider.GetByCultureAsync(culture, cancellationToken);
            return language ?? defaultLanguage;
        }
    }
}
