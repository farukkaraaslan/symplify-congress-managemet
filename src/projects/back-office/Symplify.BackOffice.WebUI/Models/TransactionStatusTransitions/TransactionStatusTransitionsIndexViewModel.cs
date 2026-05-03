namespace Symplify.BackOffice.WebUI.Models.TransactionStatusTransitions;

public sealed class TransactionStatusTransitionsIndexViewModel
{
    public CreateTransactionStatusTransitionViewModel CreateTransactionStatusTransition { get; set; } = new();

    public UpdateTransactionStatusTransitionViewModel UpdateTransactionStatusTransition { get; set; } = new();

    public IReadOnlyList<TransactionStatusSelectItemViewModel> StatusOptions { get; set; } = Array.Empty<TransactionStatusSelectItemViewModel>();
}
