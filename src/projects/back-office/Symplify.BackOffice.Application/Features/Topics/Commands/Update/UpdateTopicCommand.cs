using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Topics.Constants;
using Symplify.BackOffice.Application.Features.Topics.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Topics.Commands.Update;

public class UpdateTopicCommand : IRequest<UpdatedTopicResponse>, ISecuredRequest, ICacheRemoverRequest
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

    public string CacheGroupKey => "GetTopics";

    public string[] Roles => new[] { TopicsOperationClaims.Admin, TopicsOperationClaims.Write, TopicsOperationClaims.Update };

    public class UpdateTopicCommandHandler : IRequestHandler<UpdateTopicCommand, UpdatedTopicResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITopicRepository _repository;
        private readonly ITopicTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly TopicBusinessRules _rules;

        public UpdateTopicCommandHandler(
            ITopicRepository repository,
            ITopicTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            TopicBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedTopicResponse> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationNamesShouldBeUniqueInRequest(request.Translations);
            await _rules.TranslationNamesShouldBeUniqueInDatabase(request.Translations, excludedTopicId: request.Id);

            Topic? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.TopicShouldExistWhenSelected(entity);

            entity!.Code = request.Code;
            entity.IsActive = request.IsActive;

            await NormalizeVisibleOrdersAsync(entity, request.Order, cancellationToken);

            Topic updatedEntity = await _repository.UpdateAsync(entity);
            await UpsertTranslationsAsync(request.Id, request.Translations, cancellationToken);

            return _mapper.Map<UpdatedTopicResponse>(updatedEntity);
        }

        private async Task UpsertTranslationsAsync(
            Guid rootId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            List<TopicTranslation> existingTranslations = _translationRepository.Query()
                .ToList()
                .Where(translation => EqualityComparer<Guid>.Default.Equals(translation.TopicId, rootId) && !IsDeleted(translation))
                .ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                TopicTranslation? existingTranslation = existingTranslations
                    .FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    TopicTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TopicId", rootId);
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
            Topic currentEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<Topic> entities = _repository.Query()
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
            IReadOnlyList<Topic> entities,
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
