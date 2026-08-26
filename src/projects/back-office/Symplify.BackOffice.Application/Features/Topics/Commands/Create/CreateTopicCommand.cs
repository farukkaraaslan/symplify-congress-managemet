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

namespace Symplify.BackOffice.Application.Features.Topics.Commands.Create;

public class CreateTopicCommand : IRequest<CreatedTopicResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string? Code { get; set; }

    /// <summary>
    /// 0 veya negatif gönderilirse kayıt listenin sonuna eklenir.
    /// Pozitif gönderilirse hedef sıraya yerleştirilir ve aktif liste normalize edilir.
    /// </summary>
    public int Order { get; set; }

    public bool IsActive { get; set; }

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTopics";

    public string[] Roles => new[] { TopicsOperationClaims.Admin, TopicsOperationClaims.Write, TopicsOperationClaims.Add };

    public class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, CreatedTopicResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ITopicRepository _repository;
        private readonly ITopicTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly TopicBusinessRules _rules;

        public CreateTopicCommandHandler(
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

        public async Task<CreatedTopicResponse> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationNamesShouldBeUniqueInRequest(request.Translations);
            await _rules.TranslationNamesShouldBeUniqueInDatabase(request.Translations, excludedTopicId: null);

            Topic entity = new()
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Order = 0,
                IsActive = request.IsActive
            };

            Topic createdEntity = await _repository.AddAsync(entity);

            await NormalizeVisibleOrdersAsync(createdEntity, request.Order, cancellationToken);
            await CreateTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedTopicResponse>(createdEntity);
        }

        private async Task CreateTranslationsAsync(
            Guid rootId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
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

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                TopicTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "TopicId", rootId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }

        private async Task NormalizeVisibleOrdersAsync(
            Topic createdEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<Topic> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.Id != createdEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = NormalizeTargetOrder(requestedOrder, entities.Count + 1);
            entities.Insert(targetOrder - 1, createdEntity);

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
