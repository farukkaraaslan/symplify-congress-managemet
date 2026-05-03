namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.DeleteTranslation;

public class DeletedTransactionStatusTranslationResponse
{
    public Guid Id { get; set; }

    public int TransactionStatusId { get; set; }

    public Guid LanguageId { get; set; }
}
