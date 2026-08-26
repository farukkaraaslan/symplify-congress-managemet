namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Update;
public class UpdatedCongressEvaluationCriterionResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
