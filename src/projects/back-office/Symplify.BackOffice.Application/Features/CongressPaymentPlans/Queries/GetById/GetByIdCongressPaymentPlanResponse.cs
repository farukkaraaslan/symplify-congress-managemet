namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetById;

public class GetByIdCongressPaymentPlanResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string AudienceType { get; set; } = string.Empty;
    public string PaymentCategory { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int Order { get; set; }
    public bool IsPublicVisible { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}
