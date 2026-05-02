using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetForUpdate;
public class GetCongressPaymentPlanForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
