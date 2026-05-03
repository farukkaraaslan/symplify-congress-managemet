namespace Symplify.BackOffice.WebUI.Models.TransactionStatusTransitions;

public sealed class CreateTransactionStatusTransitionViewModel
{
    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateTransactionStatusTransitionTranslationViewModel> Translations { get; set; } = new();
}
