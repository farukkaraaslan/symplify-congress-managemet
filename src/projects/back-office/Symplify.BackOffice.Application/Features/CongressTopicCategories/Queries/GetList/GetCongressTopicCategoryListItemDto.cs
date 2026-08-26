namespace Symplify.BackOffice.Application.Features.CongressTopicCategories.Queries.GetList;

public sealed class GetCongressTopicCategoryTranslationDto
{
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class GetCongressTopicCategoryListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool IsFallback { get; set; }
    public IReadOnlyList<GetCongressTopicCategoryTranslationDto> Translations { get; set; }
        = Array.Empty<GetCongressTopicCategoryTranslationDto>();
}
