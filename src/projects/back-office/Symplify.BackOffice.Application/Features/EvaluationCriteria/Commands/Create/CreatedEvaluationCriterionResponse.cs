namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Create;
public class CreatedEvaluationCriterionResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
