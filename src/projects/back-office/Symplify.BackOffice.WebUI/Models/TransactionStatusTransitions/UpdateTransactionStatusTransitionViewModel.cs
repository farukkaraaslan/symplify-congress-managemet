namespace Symplify.BackOffice.WebUI.Models.TransactionStatusTransitions;

public sealed class UpdateTransactionStatusTransitionViewModel
{
    public int Id { get; set; }

    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateTransactionStatusTransitionTranslationViewModel> Translations { get; set; } = new();
}
