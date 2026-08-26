using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressTopicCategories.Queries.GetList;

public sealed class GetCongressTopicCategoryListQuery
    : IRequest<IReadOnlyList<GetCongressTopicCategoryListItemDto>>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[]
    {
        CongressTopicsOperationClaims.Admin,
        CongressTopicsOperationClaims.Read
    };

    public sealed class Handler
        : IRequestHandler<GetCongressTopicCategoryListQuery, IReadOnlyList<GetCongressTopicCategoryListItemDto>>
    {
        private readonly ICongressTopicCategoryRepository _categoryRepository;
        private readonly ICongressTopicCategoryTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public Handler(
            ICongressTopicCategoryRepository categoryRepository,
            ICongressTopicCategoryTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _categoryRepository = categoryRepository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<IReadOnlyList<GetCongressTopicCategoryListItemDto>> Handle(
            GetCongressTopicCategoryListQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            List<CongressTopicCategory> categories = _categoryRepository
                .Query()
                .ToList()
                .Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            HashSet<Guid> categoryIds = categories.Select(entity => entity.Id).ToHashSet();
            List<CongressTopicCategoryTranslation> translations = categoryIds.Count == 0
                ? new List<CongressTopicCategoryTranslation>()
                : _translationRepository
                    .Query()
                    .ToList()
                    .Where(entity => categoryIds.Contains(entity.CongressTopicCategoryId) && !IsDeleted(entity))
                    .ToList();

            return categories.Select(category =>
            {
                List<CongressTopicCategoryTranslation> categoryTranslations = translations
                    .Where(item => item.CongressTopicCategoryId == category.Id)
                    .ToList();

                CongressTopicCategoryTranslation? requestedTranslation = categoryTranslations
                    .FirstOrDefault(item => item.LanguageId == requestedLanguage.Id);

                CongressTopicCategoryTranslation? displayTranslation = _fallbackResolver.Resolve(
                    categoryTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id);

                return new GetCongressTopicCategoryListItemDto
                {
                    Id = category.Id,
                    CongressId = category.CongressId,
                    Name = displayTranslation?.Name ?? string.Empty,
                    Order = category.Order,
                    IsActive = category.IsActive,
                    IsFallback = requestedTranslation is null && displayTranslation is not null,
                    Translations = categoryTranslations
                        .OrderBy(item => item.LanguageId)
                        .Select(item => new GetCongressTopicCategoryTranslationDto
                        {
                            LanguageId = item.LanguageId,
                            Name = item.Name
                        })
                        .ToArray()
                };
            }).ToArray();
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
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
