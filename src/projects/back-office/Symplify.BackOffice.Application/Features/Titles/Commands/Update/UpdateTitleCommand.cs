using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Titles.Constants;
using Symplify.BackOffice.Application.Features.Titles.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Titles.Commands.Update;

public class UpdateTitleCommand : IRequest<UpdatedTitleResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    /// <summary>
    /// 0 veya negatif gönderilirse mevcut sıra korunur.
    /// Pozitif gönderilirse kayıt hedef sıraya taşınır ve aktif liste normalize edilir.
    /// </summary>
    public int Order { get; set; }

    public bool IsActive { get; set; }

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTitles";

    public string[] Roles => new[] { TitlesOperationClaims.Admin, TitlesOperationClaims.Write, TitlesOperationClaims.Update };

    public class UpdateTitleCommandHandler : IRequestHandler<UpdateTitleCommand, UpdatedTitleResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITitleRepository _repository;
        private readonly ITitleTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly TitleBusinessRules _rules;

        public UpdateTitleCommandHandler(
            ITitleRepository repository,
            ITitleTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            TitleBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedTitleResponse> Handle(UpdateTitleCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);

            Title? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.TitleShouldExistWhenSelected(entity);

            entity!.Code = request.Code;
            entity.IsActive = request.IsActive;

            await NormalizeVisibleOrdersAsync(entity, request.Order, cancellationToken);

            Title updatedEntity = await _repository.UpdateAsync(entity);
            await UpsertTranslationsAsync(request.Id, request.Translations, cancellationToken);

            return _mapper.Map<UpdatedTitleResponse>(updatedEntity);
        }

        private async Task UpsertTranslationsAsync(
            Guid rootId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            List<TitleTranslation> existingTranslations = _translationRepository.Query()
                .ToList()
                .Where(translation => EqualityComparer<Guid>.Default.Equals(translation.TitleId, rootId) && !IsDeleted(translation))
                .ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                TitleTranslation? existingTranslation = existingTranslations
                    .FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    TitleTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TitleId", rootId);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                    await _translationRepository.AddAsync(translation);
                    continue;
                }

                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
        }

        private async Task NormalizeVisibleOrdersAsync(
            Title currentEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<Title> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.Id != currentEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = requestedOrder > 0
                ? NormalizeTargetOrder(requestedOrder, entities.Count + 1)
                : NormalizeTargetOrder(currentEntity.Order, entities.Count + 1);

            entities.Insert(targetOrder - 1, currentEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<Title> entities,
            CancellationToken cancellationToken)
        {
            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static int NormalizeTargetOrder(int requestedOrder, int maxOrder)
        {
            if (requestedOrder <= 0)
                return maxOrder;

            return requestedOrder > maxOrder
                ? maxOrder
                : requestedOrder;
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
