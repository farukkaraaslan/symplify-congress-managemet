namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplateTransitions;

public sealed class CreateWorkflowTemplateTransitionViewModel
{
    public Guid WorkflowTemplateId { get; set; }

    public int TransactionStatusTransitionId { get; set; }

    public bool IsActive { get; set; } = true;
}
