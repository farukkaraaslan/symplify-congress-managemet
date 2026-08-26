namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Queries.GetList;

public class GetListWorkflowTemplateTransitionListItemDto
{
    public Guid Id { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public string FromStatusName { get; set; } = string.Empty;
    public string ToStatusName { get; set; } = string.Empty;
    public string TransitionName { get; set; } = string.Empty;
    public string? TransitionDescription { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
