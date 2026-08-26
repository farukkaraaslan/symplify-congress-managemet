using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetSelectionList;

public sealed class GetCongressTopicSelectionListQuery
    : IRequest<IReadOnlyList<GetCongressTopicSelectionListItemDto>>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[] { CongressTopicsOperationClaims.Admin, CongressTopicsOperationClaims.Read };

    public sealed class GetCongressTopicSelectionListQueryHandler
        : IRequestHandler<GetCongressTopicSelectionListQuery, IReadOnlyList<GetCongressTopicSelectionListItemDto>>
    {
        private readonly ICongressTopicRepository _congressTopicRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly ITopicTranslationRepository _topicTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetCongressTopicSelectionListQueryHandler(
            ICongressTopicRepository congressTopicRepository,
            ITopicRepository topicRepository,
            ITopicTranslationRepository topicTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _congressTopicRepository = congressTopicRepository;
            _topicRepository = topicRepository;
            _topicTranslationRepository = topicTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<IReadOnlyList<GetCongressTopicSelectionListItemDto>> Handle(
            GetCongressTopicSelectionListQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<CongressTopic> selectedRelations = _congressTopicRepository
                .Query()
                .ToList()
                .Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity))
                .ToList();

            HashSet<Guid> selectedTopicIds = selectedRelations.Select(entity => entity.TopicId).ToHashSet();
            Dictionary<Guid, CongressTopic> selectedRelationByTopicId = selectedRelations
                .GroupBy(entity => entity.TopicId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(entity => entity.Id).First());

            List<Topic> topics = _topicRepository
                .Query()
                .ToList()
                .Where(topic => !IsDeleted(topic) && (topic.IsActive || selectedTopicIds.Contains(topic.Id)))
                .OrderBy(topic => topic.Order <= 0 ? int.MaxValue : topic.Order)
                .ThenBy(topic => topic.Id)
                .ToList();

            HashSet<Guid> topicIds = topics.Select(topic => topic.Id).ToHashSet();

            List<TopicTranslation> translations = topicIds.Count == 0
                ? new List<TopicTranslation>()
                : _topicTranslationRepository
                    .Query()
                    .ToList()
                    .Where(translation => topicIds.Contains(translation.TopicId) && !IsDeleted(translation))
                    .ToList();

            return topics.Select(topic => Project(
                    topic,
                    selectedRelationByTopicId,
                    translations,
                    requestedLanguage.Id,
                    defaultLanguage.Id))
                .ToList();
        }

        private GetCongressTopicSelectionListItemDto Project(
            Topic topic,
            IReadOnlyDictionary<Guid, CongressTopic> selectedRelationByTopicId,
            IEnumerable<TopicTranslation> translations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            List<TopicTranslation> topicTranslations = translations
                .Where(translation => translation.TopicId == topic.Id)
                .ToList();

            TopicTranslation? requestedTranslation = topicTranslations
                .FirstOrDefault(translation => translation.LanguageId == requestedLanguageId);

            TopicTranslation? displayTranslation = _fallbackResolver.Resolve(
                topicTranslations,
                requestedLanguageId,
                defaultLanguageId);

            bool isSelected = selectedRelationByTopicId.TryGetValue(topic.Id, out CongressTopic? relation);

            return new GetCongressTopicSelectionListItemDto
            {
                TopicId = topic.Id,
                CongressTopicId = relation?.Id,
                CategoryId = relation?.CategoryId,
                Code = topic.Code,
                Name = displayTranslation is null
                    ? string.Empty
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name") ?? string.Empty,
                Description = displayTranslation is null
                    ? null
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                Order = topic.Order,
                IsActive = topic.IsActive,
                IsSelected = isSelected,
                IsFallback = requestedTranslation is null && displayTranslation is not null
            };
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(
            Guid? languageId,
            string? culture,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
