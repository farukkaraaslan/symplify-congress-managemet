namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.DeleteTranslation;
public class DeletedTransactionStatusTransitionTranslationResponse
{
    public Guid Id { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public Guid LanguageId { get; set; }
}
