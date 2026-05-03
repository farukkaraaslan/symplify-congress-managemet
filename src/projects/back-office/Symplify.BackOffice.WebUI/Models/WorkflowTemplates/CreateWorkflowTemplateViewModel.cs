namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplates;

public sealed class CreateWorkflowTemplateViewModel
{
    public string? Code { get; set; }

    public int? InitialTransactionStatusId { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateWorkflowTemplateTranslationViewModel> Translations { get; set; } = new();
}
