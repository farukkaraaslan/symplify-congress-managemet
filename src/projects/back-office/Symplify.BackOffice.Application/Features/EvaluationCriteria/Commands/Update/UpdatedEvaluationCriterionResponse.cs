namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Update;
public class UpdatedEvaluationCriterionResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
