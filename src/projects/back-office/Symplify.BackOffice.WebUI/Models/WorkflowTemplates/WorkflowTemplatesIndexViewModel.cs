namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplates;

public sealed class WorkflowTemplatesIndexViewModel
{
    public List<TransactionStatusSelectItemViewModel> InitialStatuses { get; set; } = new();

    public CreateWorkflowTemplateViewModel CreateWorkflowTemplate { get; set; } = new();

    public UpdateWorkflowTemplateViewModel UpdateWorkflowTemplate { get; set; } = new();
}
