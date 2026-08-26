using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressTopicCategories.Commands.Save;

public sealed class SaveCongressTopicCategoryTranslationDto
{
    public Guid LanguageId { get; set; }
    public string? Name { get; set; }
}

public sealed class SaveCongressTopicCategoryItemDto
{
    public Guid? Id { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SaveCongressTopicCategoryTranslationDto> Translations { get; set; }
        = new List<SaveCongressTopicCategoryTranslationDto>();
}

public sealed class SavedCongressTopicCategoriesResponse
{
    public int CategoryCount { get; set; }
}

public sealed class SaveCongressTopicCategoriesCommand
    : IRequest<SavedCongressTopicCategoriesResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public ICollection<SaveCongressTopicCategoryItemDto> Categories { get; set; }
        = new List<SaveCongressTopicCategoryItemDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressTopics";

    public string[] Roles => new[]
    {
        CongressTopicsOperationClaims.Admin,
        CongressTopicsOperationClaims.Write,
        CongressTopicsOperationClaims.Update
    };

    public sealed class Handler
        : IRequestHandler<SaveCongressTopicCategoriesCommand, SavedCongressTopicCategoriesResponse>
    {
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressTopicCategoryRepository _categoryRepository;
        private readonly ICongressTopicCategoryTranslationRepository _translationRepository;
        private readonly ICongressTopicRepository _congressTopicRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public Handler(
            ICongressRepository congressRepository,
            ICongressTopicCategoryRepository categoryRepository,
            ICongressTopicCategoryTranslationRepository translationRepository,
            ICongressTopicRepository congressTopicRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _congressRepository = congressRepository;
            _categoryRepository = categoryRepository;
            _translationRepository = translationRepository;
            _congressTopicRepository = congressTopicRepository;
            _languageProvider = languageProvider;
        }

        public async Task<SavedCongressTopicCategoriesResponse> Handle(
            SaveCongressTopicCategoriesCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CongressId == Guid.Empty)
                throw new BusinessException(CongressTopicsMessages.CongressRequired);

            Congress? congress = await _congressRepository.GetAsync(
                predicate: item => item.Id == request.CongressId,
                cancellationToken: cancellationToken);

            if (congress is null)
                throw new BusinessException(CongressTopicsMessages.CongressNotFound);

            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(item => item.Id).ToHashSet();

            List<CongressTopicCategory> existingCategories = _categoryRepository
                .Query()
                .ToList()
                .Where(item => item.CongressId == request.CongressId && !IsDeleted(item))
                .ToList();

            Dictionary<Guid, CongressTopicCategory> existingById = existingCategories.ToDictionary(item => item.Id);
            HashSet<Guid> incomingExistingIds = request.Categories
                .Where(item => item.Id.HasValue && item.Id.Value != Guid.Empty)
                .Select(item => item.Id!.Value)
                .ToHashSet();

            if (incomingExistingIds.Any(id => !existingById.ContainsKey(id)))
                throw new BusinessException("BackOffice.CongressTopics.Validation.CategoryNotFound");

            foreach (CongressTopicCategory removedCategory in existingCategories
                         .Where(item => !incomingExistingIds.Contains(item.Id)))
            {
                List<CongressTopic> affectedTopics = _congressTopicRepository
                    .Query()
                    .ToList()
                    .Where(item => item.CongressId == request.CongressId &&
                                   item.CategoryId == removedCategory.Id &&
                                   !IsDeleted(item))
                    .ToList();

                foreach (CongressTopic relation in affectedTopics)
                {
                    relation.CategoryId = null;
                    await _congressTopicRepository.UpdateAsync(relation);
                }

                List<CongressTopicCategoryTranslation> removedTranslations = _translationRepository
                    .Query()
                    .ToList()
                    .Where(item => item.CongressTopicCategoryId == removedCategory.Id && !IsDeleted(item))
                    .ToList();

                foreach (CongressTopicCategoryTranslation translation in removedTranslations)
                    await _translationRepository.DeleteAsync(translation);

                await _categoryRepository.DeleteAsync(removedCategory);
            }

            int index = 0;
            foreach (SaveCongressTopicCategoryItemDto input in request.Categories)
            {
                index++;
                List<SaveCongressTopicCategoryTranslationDto> translations = input.Translations
                    .Where(item => item.LanguageId != Guid.Empty && activeLanguageIds.Contains(item.LanguageId))
                    .GroupBy(item => item.LanguageId)
                    .Select(group => group.First())
                    .ToList();

                string? defaultName = Normalize(translations
                    .FirstOrDefault(item => item.LanguageId == defaultLanguage.Id)?.Name);

                if (string.IsNullOrWhiteSpace(defaultName))
                    throw new BusinessException("BackOffice.CongressTopics.Validation.DefaultCategoryNameRequired");

                if (translations.Any(item => Normalize(item.Name)?.Length > 200))
                    throw new BusinessException("BackOffice.CongressTopics.Validation.CategoryNameTooLong");

                CongressTopicCategory category;
                if (input.Id.HasValue && input.Id.Value != Guid.Empty)
                {
                    category = existingById[input.Id.Value];
                    category.Order = input.Order > 0 ? input.Order : index;
                    category.IsActive = input.IsActive;
                    await _categoryRepository.UpdateAsync(category);
                }
                else
                {
                    category = new CongressTopicCategory
                    {
                        Id = Guid.NewGuid(),
                        CongressId = request.CongressId,
                        Order = input.Order > 0 ? input.Order : index,
                        IsActive = input.IsActive
                    };

                    await _categoryRepository.AddAsync(category);
                }

                await UpsertTranslationsAsync(category.Id, translations, cancellationToken);
            }

            return new SavedCongressTopicCategoriesResponse
            {
                CategoryCount = request.Categories.Count
            };
        }

        private async Task UpsertTranslationsAsync(
            Guid categoryId,
            IReadOnlyCollection<SaveCongressTopicCategoryTranslationDto> inputs,
            CancellationToken cancellationToken)
        {
            List<CongressTopicCategoryTranslation> existing = _translationRepository
                .Query()
                .ToList()
                .Where(item => item.CongressTopicCategoryId == categoryId && !IsDeleted(item))
                .ToList();

            Dictionary<Guid, CongressTopicCategoryTranslation> existingByLanguage = existing
                .GroupBy(item => item.LanguageId)
                .ToDictionary(group => group.Key, group => group.First());

            HashSet<Guid> retainedLanguageIds = new();

            foreach (SaveCongressTopicCategoryTranslationDto input in inputs)
            {
                string? name = Normalize(input.Name);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                retainedLanguageIds.Add(input.LanguageId);

                if (existingByLanguage.TryGetValue(input.LanguageId, out CongressTopicCategoryTranslation? translation))
                {
                    translation.Name = name;
                    await _translationRepository.UpdateAsync(translation);
                    continue;
                }

                await _translationRepository.AddAsync(new CongressTopicCategoryTranslation
                {
                    Id = Guid.NewGuid(),
                    CongressTopicCategoryId = categoryId,
                    LanguageId = input.LanguageId,
                    Name = name
                });
            }

            foreach (CongressTopicCategoryTranslation translation in existing
                         .Where(item => !retainedLanguageIds.Contains(item.LanguageId)))
                await _translationRepository.DeleteAsync(translation);
        }

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
