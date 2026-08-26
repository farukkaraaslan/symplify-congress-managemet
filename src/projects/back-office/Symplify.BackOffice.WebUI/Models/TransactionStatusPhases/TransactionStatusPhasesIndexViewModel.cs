namespace Symplify.BackOffice.WebUI.Models.TransactionStatusPhases;

public sealed class TransactionStatusPhasesIndexViewModel
{
    public CreateTransactionStatusPhaseViewModel CreateTransactionStatusPhase { get; set; } = new();

    public UpdateTransactionStatusPhaseViewModel UpdateTransactionStatusPhase { get; set; } = new();
}
