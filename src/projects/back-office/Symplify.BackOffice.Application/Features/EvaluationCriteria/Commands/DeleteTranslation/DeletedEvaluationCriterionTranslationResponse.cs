namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.DeleteTranslation;
public class DeletedEvaluationCriterionTranslationResponse
{
    public Guid Id { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public Guid LanguageId { get; set; }
}
