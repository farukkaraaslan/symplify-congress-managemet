namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Update;
public class UpdatedPaymentStatusResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
