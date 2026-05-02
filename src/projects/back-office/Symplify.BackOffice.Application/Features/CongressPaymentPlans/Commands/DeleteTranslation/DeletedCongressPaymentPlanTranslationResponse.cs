namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.DeleteTranslation;
public class DeletedCongressPaymentPlanTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressPaymentPlanId { get; set; }
    public Guid LanguageId { get; set; }
}
