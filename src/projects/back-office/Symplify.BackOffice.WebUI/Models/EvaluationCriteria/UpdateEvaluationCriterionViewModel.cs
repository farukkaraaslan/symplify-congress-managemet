namespace Symplify.BackOffice.WebUI.Models.EvaluationCriteria;

public sealed class UpdateEvaluationCriterionViewModel
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateEvaluationCriterionTranslationViewModel> Translations { get; set; } = new();
}
