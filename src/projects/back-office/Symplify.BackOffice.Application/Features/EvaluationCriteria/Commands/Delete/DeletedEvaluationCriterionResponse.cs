namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Delete;
public class DeletedEvaluationCriterionResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
