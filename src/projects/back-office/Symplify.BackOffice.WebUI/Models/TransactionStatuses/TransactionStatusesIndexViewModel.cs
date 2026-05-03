namespace Symplify.BackOffice.WebUI.Models.TransactionStatuses;

public sealed class TransactionStatusesIndexViewModel
{
    public List<TransactionStatusPhaseSelectItemViewModel> Phases { get; set; } = new();

    public CreateTransactionStatusViewModel CreateTransactionStatus { get; set; } = new();

    public UpdateTransactionStatusViewModel UpdateTransactionStatus { get; set; } = new();
}
