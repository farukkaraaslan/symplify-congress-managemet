using Core.Application.Pipelines.Authorization;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetCreatePage;

public sealed class GetSubmissionCreatePageQuery : IRequest<GetSubmissionCreatePageResponse>, ISecuredRequest
{
    public Guid? CongressId { get; set; }

    public string? Culture { get; set; }

    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Read, SubmissionsOperationClaims.Add };

    public sealed class GetSubmissionCreatePageQueryHandler : IRequestHandler<GetSubmissionCreatePageQuery, GetSubmissionCreatePageResponse>
    {
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressTranslationRepository _congressTranslationRepository;
        private readonly ICongressSubmissionTypeRepository _congressSubmissionTypeRepository;
        private readonly ISubmissionTypeTranslationRepository _submissionTypeTranslationRepository;
        private readonly ICongressTopicRepository _congressTopicRepository;
        private readonly ITopicTranslationRepository _topicTranslationRepository;
        private readonly ILanguageRepository _languageRepository;
        private readonly ITitleRepository _titleRepository;
        private readonly ITitleTranslationRepository _titleTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetSubmissionCreatePageQueryHandler(
            ICongressRepository congressRepository,
            ICongressTranslationRepository congressTranslationRepository,
            ICongressSubmissionTypeRepository congressSubmissionTypeRepository,
            ISubmissionTypeTranslationRepository submissionTypeTranslationRepository,
            ICongressTopicRepository congressTopicRepository,
            ITopicTranslationRepository topicTranslationRepository,
            ILanguageRepository languageRepository,
            ITitleRepository titleRepository,
            ITitleTranslationRepository titleTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _congressRepository = congressRepository;
            _congressTranslationRepository = congressTranslationRepository;
            _congressSubmissionTypeRepository = congressSubmissionTypeRepository;
            _submissionTypeTranslationRepository = submissionTypeTranslationRepository;
            _congressTopicRepository = congressTopicRepository;
            _topicTranslationRepository = topicTranslationRepository;
            _languageRepository = languageRepository;
            _titleRepository = titleRepository;
            _titleTranslationRepository = titleTranslationRepository;
            _languageProvider = languageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetSubmissionCreatePageResponse> Handle(GetSubmissionCreatePageQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = !string.IsNullOrWhiteSpace(request.Culture)
                ? await _languageProvider.GetByCultureAsync(request.Culture, cancellationToken) ?? defaultLanguage
                : defaultLanguage;

            List<Congress> congresses = _congressRepository
                .Query()
                .ToList()
                .Where(entity => !IsDeleted(entity))
                .OrderByDescending(entity => entity.StartDate)
                .ThenBy(entity => entity.Name)
                .ToList();

            Guid? selectedCongressId = request.CongressId.HasValue && request.CongressId.Value != Guid.Empty
                ? request.CongressId.Value
                : congresses.FirstOrDefault()?.Id;

            List<CongressTranslation> congressTranslations = _congressTranslationRepository.Query().ToList();

            IReadOnlyList<SubmissionCreateSelectItemDto> congressItems = congresses
                .Select(congress => new SubmissionCreateSelectItemDto
                {
                    Id = congress.Id,
                    Text = ResolveCongressTitle(congress, congressTranslations, requestedLanguage.Id, defaultLanguage.Id)
                })
                .ToList();

            IReadOnlyList<SubmissionCreateSelectItemDto> submissionTypeItems = selectedCongressId.HasValue
                ? BuildSubmissionTypeItems(selectedCongressId.Value, requestedLanguage.Id, defaultLanguage.Id)
                : Array.Empty<SubmissionCreateSelectItemDto>();

            IReadOnlyList<SubmissionCreateSelectItemDto> topicItems = selectedCongressId.HasValue
                ? BuildTopicItems(selectedCongressId.Value, requestedLanguage.Id, defaultLanguage.Id)
                : Array.Empty<SubmissionCreateSelectItemDto>();

            List<Language> languages = _languageRepository
                .Query()
                .ToList()
                .Where(language => language.IsActive && !IsDeleted(language))
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .ToList();

            IReadOnlyList<SubmissionCreateSelectItemDto> titleItems = BuildTitleItems(requestedLanguage.Id, defaultLanguage.Id);

            return new GetSubmissionCreatePageResponse
            {
                SelectedCongressId = selectedCongressId,
                DefaultLanguageId = languages.FirstOrDefault(language => language.IsDefault)?.Id,
                Congresses = congressItems,
                SubmissionTypes = submissionTypeItems,
                Topics = topicItems,
                Languages = languages.Select(language => new SubmissionCreateSelectItemDto
                {
                    Id = language.Id,
                    Text = language.Name
                }).ToList(),
                Titles = titleItems
            };
        }

        private IReadOnlyList<SubmissionCreateSelectItemDto> BuildSubmissionTypeItems(Guid congressId, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<CongressSubmissionType> relations = _congressSubmissionTypeRepository
                .Query()
                .Include(entity => entity.SubmissionType)
                .ToList()
                .Where(entity => entity.CongressId == congressId && entity.IsActive && !IsDeleted(entity) && entity.SubmissionType.IsActive && !IsDeleted(entity.SubmissionType))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            HashSet<Guid> submissionTypeIds = relations.Select(entity => entity.SubmissionTypeId).ToHashSet();
            List<SubmissionTypeTranslation> translations = _submissionTypeTranslationRepository
                .Query()
                .ToList()
                .Where(translation => submissionTypeIds.Contains(translation.SubmissionTypeId) && !IsDeleted(translation))
                .ToList();

            return relations.Select(relation =>
            {
                List<SubmissionTypeTranslation> itemTranslations = translations
                    .Where(translation => translation.SubmissionTypeId == relation.SubmissionTypeId)
                    .ToList();

                SubmissionTypeTranslation? translation = _fallbackResolver.Resolve(itemTranslations, requestedLanguageId, defaultLanguageId);

                return new SubmissionCreateSelectItemDto
                {
                    Id = relation.SubmissionTypeId,
                    Text = translation?.Name ?? relation.SubmissionTypeId.ToString(),
                    FormProfile = relation.SubmissionType.FormProfile
                };
            }).ToList();
        }

        private IReadOnlyList<SubmissionCreateSelectItemDto> BuildTopicItems(Guid congressId, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<CongressTopic> relations = _congressTopicRepository
                .Query()
                .ToList()
                .Where(entity => entity.CongressId == congressId && entity.IsActive && !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            HashSet<Guid> topicIds = relations.Select(entity => entity.TopicId).ToHashSet();
            List<TopicTranslation> translations = _topicTranslationRepository
                .Query()
                .ToList()
                .Where(translation => topicIds.Contains(translation.TopicId) && !IsDeleted(translation))
                .ToList();

            return relations.Select(relation =>
            {
                List<TopicTranslation> itemTranslations = translations
                    .Where(translation => translation.TopicId == relation.TopicId)
                    .ToList();

                TopicTranslation? translation = _fallbackResolver.Resolve(itemTranslations, requestedLanguageId, defaultLanguageId);

                return new SubmissionCreateSelectItemDto
                {
                    Id = relation.TopicId,
                    Text = translation?.Name ?? relation.TopicId.ToString()
                };
            }).ToList();
        }


        private IReadOnlyList<SubmissionCreateSelectItemDto> BuildTitleItems(Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<Title> titles = _titleRepository
                .Query()
                .ToList()
                .Where(title => title.IsActive && !IsDeleted(title))
                .OrderBy(title => title.Order <= 0 ? int.MaxValue : title.Order)
                .ThenBy(title => title.Code)
                .ThenBy(title => title.Id)
                .ToList();

            HashSet<Guid> titleIds = titles.Select(title => title.Id).ToHashSet();
            List<TitleTranslation> translations = _titleTranslationRepository
                .Query()
                .ToList()
                .Where(translation => titleIds.Contains(translation.TitleId) && !IsDeleted(translation))
                .ToList();

            return titles.Select(title =>
            {
                List<TitleTranslation> itemTranslations = translations
                    .Where(translation => translation.TitleId == title.Id)
                    .ToList();

                TitleTranslation? translation = _fallbackResolver.Resolve(itemTranslations, requestedLanguageId, defaultLanguageId);

                return new SubmissionCreateSelectItemDto
                {
                    Id = title.Id,
                    Text = ResolveTitleDisplayText(title, translation)
                };
            }).ToList();
        }

        private static string ResolveTitleDisplayText(Title title, TitleTranslation? translation)
        {
            if (!string.IsNullOrWhiteSpace(translation?.Description))
                return translation.Description.Trim();

            if (!string.IsNullOrWhiteSpace(translation?.Name))
                return translation.Name.Trim();

            return title.Code ?? title.Id.ToString();
        }

        private string ResolveCongressTitle(Congress congress, List<CongressTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<CongressTranslation> rootTranslations = translations
                .Where(translation => translation.CongressId == congress.Id && !IsDeleted(translation))
                .ToList();

            CongressTranslation? translation = _fallbackResolver.Resolve(rootTranslations, requestedLanguageId, defaultLanguageId);
            return !string.IsNullOrWhiteSpace(translation?.Title)
                ? translation.Title
                : congress.Name;
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = entity.GetType().GetProperty("DeletedDate")?.GetValue(entity);
            return deletedDate is not null;
        }
    }
}
