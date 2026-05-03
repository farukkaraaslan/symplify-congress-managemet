namespace Symplify.BackOffice.WebUI.Models.TransactionStatusTransitions;

public sealed class TransactionStatusSelectItemViewModel
{
    public int Id { get; set; }

    public int TransactionStatusPhaseId { get; set; }

    public string TransactionStatusPhaseCode { get; set; } = string.Empty;

    public string TransactionStatusPhaseName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DisplayText
    {
        get
        {
            string statusText = string.IsNullOrWhiteSpace(Code)
                ? Name
                : string.IsNullOrWhiteSpace(Name)
                    ? Code
                    : $"{Name} ({Code})";

            if (string.IsNullOrWhiteSpace(TransactionStatusPhaseName))
                return statusText;

            return $"{TransactionStatusPhaseName} / {statusText}";
        }
    }
}
