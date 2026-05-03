namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.DeleteTranslation;

public class DeletedTransactionStatusPhaseTranslationResponse
{
    public Guid Id { get; set; }

    public int TransactionStatusPhaseId { get; set; }

    public Guid LanguageId { get; set; }
}
