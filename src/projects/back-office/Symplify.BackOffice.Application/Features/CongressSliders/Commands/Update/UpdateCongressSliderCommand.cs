using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Features.CongressSliders.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Update;
public class UpdateCongressSliderCommand : IRequest<UpdatedCongressSliderResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSliders";
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Write, CongressSlidersOperationClaims.Update };
    public class UpdateCongressSliderCommandHandler : IRequestHandler<UpdateCongressSliderCommand, UpdatedCongressSliderResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Title", "Subtitle", "ButtonText", "ButtonUrl" };
        private readonly ICongressSliderRepository _repository; private readonly ICongressSliderTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly CongressSliderBusinessRules _rules;
        public UpdateCongressSliderCommandHandler(ICongressSliderRepository repository, ICongressSliderTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, CongressSliderBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedCongressSliderResponse> Handle(UpdateCongressSliderCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            CongressSlider? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressSliderShouldExistWhenSelected(entity);
            entity!.CongressId = request.CongressId;
            entity!.ImagePath = request.ImagePath;
            entity!.Order = request.Order;
            entity!.IsActive = request.IsActive;
            CongressSlider updatedEntity = await _repository.UpdateAsync(entity!);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<CongressSliderTranslation> existingTranslations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressSliderId, request.Id)).ToList();
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                CongressSliderTranslation? existingTranslation = existingTranslations.FirstOrDefault(x => x.LanguageId == input.LanguageId);
                if (existingTranslation is null)
                {
                    CongressSliderTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressSliderId", request.Id);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
            return _mapper.Map<UpdatedCongressSliderResponse>(updatedEntity);
        }
    }
}
