namespace Symplify.BackOffice.WebUI.Models.EvaluationCriteria;

public sealed class CreateEvaluationCriterionViewModel
{
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateEvaluationCriterionTranslationViewModel> Translations { get; set; } = new();
}
