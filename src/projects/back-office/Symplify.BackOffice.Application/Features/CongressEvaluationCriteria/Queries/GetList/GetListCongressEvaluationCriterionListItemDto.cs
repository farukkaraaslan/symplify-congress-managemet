namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Queries.GetList;
public class GetListCongressEvaluationCriterionListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
