using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Queries.GetForUpdate;
public class GetPaymentStatusForUpdateResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
