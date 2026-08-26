namespace Symplify.BackOffice.WebUI.Models.CongressTopics;

public sealed class SaveCongressTopicCategoriesViewModel
{
    public Guid CongressId { get; set; }
    public List<SaveCongressTopicCategoryViewModel> Categories { get; set; } = new();
}

public sealed class SaveCongressTopicCategoryViewModel
{
    public Guid? Id { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SaveCongressTopicCategoryTranslationViewModel> Translations { get; set; } = new();
}

public sealed class SaveCongressTopicCategoryTranslationViewModel
{
    public Guid LanguageId { get; set; }
    public string? Name { get; set; }
}
