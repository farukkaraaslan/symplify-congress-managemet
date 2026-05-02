namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.DeleteTranslation;
public class DeletedPaymentStatusTranslationResponse
{
    public Guid Id { get; set; }
    public int PaymentStatusId { get; set; }
    public Guid LanguageId { get; set; }
}
