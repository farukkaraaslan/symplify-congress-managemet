namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Update;
public class UpdatedCongressPaymentPlanResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
