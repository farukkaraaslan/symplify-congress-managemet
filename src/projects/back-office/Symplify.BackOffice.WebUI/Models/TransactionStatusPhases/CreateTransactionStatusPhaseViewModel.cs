namespace Symplify.BackOffice.WebUI.Models.TransactionStatusPhases;

public sealed class CreateTransactionStatusPhaseViewModel
{
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateTransactionStatusPhaseTranslationViewModel> Translations { get; set; } = new();
}
