namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Create;
public class CreatedPaymentStatusResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
