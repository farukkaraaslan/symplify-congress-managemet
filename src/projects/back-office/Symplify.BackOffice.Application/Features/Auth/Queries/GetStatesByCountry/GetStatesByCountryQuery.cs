using MediatR;
using Symplify.BackOffice.Application.Features.Auth.Queries.GetRegisterOptions;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;

namespace Symplify.BackOffice.Application.Features.Auth.Queries.GetStatesByCountry;

public sealed class GetStatesByCountryQuery : IRequest<List<AuthSelectOptionDto>>
{
    public Guid CountryId { get; set; }

    public string? Culture { get; set; }

    public sealed class GetStatesByCountryQueryHandler : IRequestHandler<GetStatesByCountryQuery, List<AuthSelectOptionDto>>
    {
        private readonly IStateRepository _stateRepository;
        private readonly IStateTranslationRepository _stateTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetStatesByCountryQueryHandler(
            IStateRepository stateRepository,
            IStateTranslationRepository stateTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _stateRepository = stateRepository;
            _stateTranslationRepository = stateTranslationRepository;
            _languageProvider = languageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<List<AuthSelectOptionDto>> Handle(
            GetStatesByCountryQuery request,
            CancellationToken cancellationToken)
        {
            if (request.CountryId == Guid.Empty)
                return new List<AuthSelectOptionDto>();

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = string.IsNullOrWhiteSpace(request.Culture)
                ? defaultLanguage
                : (await _languageProvider.GetByCultureAsync(request.Culture, cancellationToken)) ?? defaultLanguage;

            List<State> states = _stateRepository.Query()
                .Where(item => item.IsActive && item.CountryId == request.CountryId)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Code)
                .ToList();

            List<StateTranslation> translations = _stateTranslationRepository.Query().ToList();

            return states.Select(state =>
            {
                List<StateTranslation> rootTranslations = translations
                    .Where(translation => translation.StateId == state.Id)
                    .ToList();

                StateTranslation? translation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                return new AuthSelectOptionDto
                {
                    Value = state.Id.ToString("D"),
                    Text = translation?.Name ?? state.Code ?? state.Id.ToString("D")
                };
            }).ToList();
        }
    }
}
