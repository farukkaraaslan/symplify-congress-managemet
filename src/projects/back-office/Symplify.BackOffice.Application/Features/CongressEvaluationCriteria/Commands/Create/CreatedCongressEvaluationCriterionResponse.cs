namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Create;
public class CreatedCongressEvaluationCriterionResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
