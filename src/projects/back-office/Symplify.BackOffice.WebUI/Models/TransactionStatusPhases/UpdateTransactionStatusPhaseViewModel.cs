namespace Symplify.BackOffice.WebUI.Models.TransactionStatusPhases;

public sealed class UpdateTransactionStatusPhaseViewModel
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateTransactionStatusPhaseTranslationViewModel> Translations { get; set; } = new();
}
