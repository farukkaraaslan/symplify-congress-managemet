namespace Symplify.BackOffice.WebUI.Models.CongressWorkflows;

public sealed class ApplyCongressWorkflowTemplateViewModel
{
    public Guid CongressId { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public bool ReplaceExistingTransitions { get; set; } = true;
}
