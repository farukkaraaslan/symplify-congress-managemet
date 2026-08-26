namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Create;

public class CreatedWorkflowTemplateTransitionResponse
{
    public Guid Id { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
