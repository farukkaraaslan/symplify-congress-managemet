namespace Symplify.BackOffice.WebUI.Models.WorkflowTemplateTransitions;

public sealed class TransactionStatusTransitionSelectItemViewModel
{
    public int Id { get; set; }

    public string FromStatusName { get; set; } = string.Empty;

    public string FromStatusCode { get; set; } = string.Empty;

    public string ToStatusName { get; set; } = string.Empty;

    public string ToStatusCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DisplayText
    {
        get
        {
            string fromText = string.IsNullOrWhiteSpace(FromStatusName) ? FromStatusCode : $"{FromStatusName} ({FromStatusCode})";
            string toText = string.IsNullOrWhiteSpace(ToStatusName) ? ToStatusCode : $"{ToStatusName} ({ToStatusCode})";
            string transitionText = string.IsNullOrWhiteSpace(Name) ? string.Empty : $" - {Name}";
            return $"{fromText} → {toText}{transitionText}";
        }
    }
}
