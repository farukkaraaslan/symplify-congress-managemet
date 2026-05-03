namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplateTransitions;

public sealed class UpdateWorkflowTemplateTransitionViewModel
{
    public Guid Id { get; set; }

    public Guid WorkflowTemplateId { get; set; }

    public int TransactionStatusTransitionId { get; set; }

    public bool IsActive { get; set; } = true;
}
