namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Queries.GetList;
public class GetListPaymentStatusListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}
