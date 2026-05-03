namespace Symplify.BackOffice.WebUI.Models.TransactionStatuses;

public sealed class UpdateTransactionStatusViewModel
{
    public int Id { get; set; }

    public int TransactionStatusPhaseId { get; set; }

    public string? Code { get; set; }

    public bool IsEditable { get; set; } = true;

    public bool IsFinal { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateTransactionStatusTranslationViewModel> Translations { get; set; } = new();
}
