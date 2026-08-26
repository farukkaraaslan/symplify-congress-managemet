namespace Symplify.BackOffice.WebUI.Models.EvaluationCriteria;

public sealed class EvaluationCriteriaIndexViewModel
{
    public CreateEvaluationCriterionViewModel CreateEvaluationCriterion { get; set; } = new();

    public UpdateEvaluationCriterionViewModel UpdateEvaluationCriterion { get; set; } = new();
}
