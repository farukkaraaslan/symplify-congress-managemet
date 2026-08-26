namespace Symplify.BackOffice.WebUI.Models.TransactionStatuses;

public sealed class TransactionStatusPhaseSelectItemViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(Name)
        ? Code
        : $"{Name} ({Code})";
}
