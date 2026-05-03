namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplateTransitions;

public sealed class WorkflowTemplateTransitionsIndexViewModel
{
    public Guid WorkflowTemplateId { get; set; }

    public string WorkflowTemplateCode { get; set; } = string.Empty;

    public string WorkflowTemplateName { get; set; } = string.Empty;

    public List<TransactionStatusTransitionSelectItemViewModel> AvailableTransitions { get; set; } = new();

    public CreateWorkflowTemplateTransitionViewModel CreateTransition { get; set; } = new();

    public UpdateWorkflowTemplateTransitionViewModel UpdateTransition { get; set; } = new();
}
