namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Queries.GetByCongressId;

public class GetCongressWorkflowByCongressIdResponse
{
    public Guid CongressId { get; set; }
    public Guid? SourceWorkflowTemplateId { get; set; }
    public int? InitialTransactionStatusId { get; set; }
    public string? InitialTransactionStatusName { get; set; }
    public bool IsActive { get; set; }
    public List<CongressWorkflowTransitionListItemDto> Transitions { get; set; } = new();
}

public class CongressWorkflowTransitionListItemDto
{
    public Guid Id { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public string FromStatusName { get; set; } = string.Empty;
    public string ToStatusName { get; set; } = string.Empty;
    public string TransitionName { get; set; } = string.Empty;
    public string? TransitionDescription { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
