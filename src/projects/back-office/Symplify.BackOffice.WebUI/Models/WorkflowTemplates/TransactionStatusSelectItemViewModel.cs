namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplates;

public sealed class TransactionStatusSelectItemViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? PhaseName { get; set; }

    public string DisplayText
    {
        get
        {
            string statusText = string.IsNullOrWhiteSpace(Name) ? Code : $"{Name} ({Code})";
            return string.IsNullOrWhiteSpace(PhaseName) ? statusText : $"{PhaseName} / {statusText}";
        }
    }
}
