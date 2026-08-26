using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetSelectionList;

public sealed class GetCongressSubmissionTypeSelectionListQuery
    : IRequest<IReadOnlyList<GetCongressSubmissionTypeSelectionListItemDto>>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[] { CongressSubmissionTypesOperationClaims.Admin, CongressSubmissionTypesOperationClaims.Read };

    public sealed class GetCongressSubmissionTypeSelectionListQueryHandler
        : IRequestHandler<GetCongressSubmissionTypeSelectionListQuery, IReadOnlyList<GetCongressSubmissionTypeSelectionListItemDto>>
    {
        private readonly ICongressSubmissionTypeRepository _congressSubmissionTypeRepository;
        private readonly ISubmissionTypeRepository _submissionTypeRepository;
        private readonly ISubmissionTypeTranslationRepository _submissionTypeTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetCongressSubmissionTypeSelectionListQueryHandler(
            ICongressSubmissionTypeRepository congressSubmissionTypeRepository,
            ISubmissionTypeRepository submissionTypeRepository,
            ISubmissionTypeTranslationRepository submissionTypeTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _congressSubmissionTypeRepository = congressSubmissionTypeRepository;
            _submissionTypeRepository = submissionTypeRepository;
            _submissionTypeTranslationRepository = submissionTypeTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<IReadOnlyList<GetCongressSubmissionTypeSelectionListItemDto>> Handle(
            GetCongressSubmissionTypeSelectionListQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<CongressSubmissionType> selectedRelations = _congressSubmissionTypeRepository
                .Query()
                .ToList()
                .Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity))
                .ToList();

            HashSet<Guid> selectedSubmissionTypeIds = selectedRelations.Select(entity => entity.SubmissionTypeId).ToHashSet();
            Dictionary<Guid, CongressSubmissionType> selectedRelationBySubmissionTypeId = selectedRelations
                .GroupBy(entity => entity.SubmissionTypeId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(entity => entity.Id).First());

            List<SubmissionType> submissionTypes = _submissionTypeRepository
                .Query()
                .ToList()
                .Where(submissionType => !IsDeleted(submissionType) && (submissionType.IsActive || selectedSubmissionTypeIds.Contains(submissionType.Id)))
                .OrderBy(submissionType => submissionType.Order <= 0 ? int.MaxValue : submissionType.Order)
                .ThenBy(submissionType => submissionType.Id)
                .ToList();

            HashSet<Guid> submissionTypeIds = submissionTypes.Select(submissionType => submissionType.Id).ToHashSet();

            List<SubmissionTypeTranslation> translations = submissionTypeIds.Count == 0
                ? new List<SubmissionTypeTranslation>()
                : _submissionTypeTranslationRepository
                    .Query()
                    .ToList()
                    .Where(translation => submissionTypeIds.Contains(translation.SubmissionTypeId) && !IsDeleted(translation))
                    .ToList();

            return submissionTypes.Select(submissionType => Project(
                    submissionType,
                    selectedRelationBySubmissionTypeId,
                    translations,
                    requestedLanguage.Id,
                    defaultLanguage.Id))
                .ToList();
        }

        private GetCongressSubmissionTypeSelectionListItemDto Project(
            SubmissionType submissionType,
            IReadOnlyDictionary<Guid, CongressSubmissionType> selectedRelationBySubmissionTypeId,
            IEnumerable<SubmissionTypeTranslation> translations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            List<SubmissionTypeTranslation> submissionTypeTranslations = translations
                .Where(translation => translation.SubmissionTypeId == submissionType.Id)
                .ToList();

            SubmissionTypeTranslation? requestedTranslation = submissionTypeTranslations
                .FirstOrDefault(translation => translation.LanguageId == requestedLanguageId);

            SubmissionTypeTranslation? displayTranslation = _fallbackResolver.Resolve(
                submissionTypeTranslations,
                requestedLanguageId,
                defaultLanguageId);

            bool isSelected = selectedRelationBySubmissionTypeId.TryGetValue(submissionType.Id, out CongressSubmissionType? relation);

            return new GetCongressSubmissionTypeSelectionListItemDto
            {
                SubmissionTypeId = submissionType.Id,
                CongressSubmissionTypeId = relation?.Id,
                Code = submissionType.Code,
                Name = displayTranslation is null
                    ? string.Empty
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name") ?? string.Empty,
                Description = displayTranslation is null
                    ? null
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                Order = submissionType.Order,
                IsActive = submissionType.IsActive,
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
