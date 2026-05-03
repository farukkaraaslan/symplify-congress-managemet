namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplates;

public sealed class UpdateWorkflowTemplateViewModel
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public int? InitialTransactionStatusId { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateWorkflowTemplateTranslationViewModel> Translations { get; set; } = new();
}
