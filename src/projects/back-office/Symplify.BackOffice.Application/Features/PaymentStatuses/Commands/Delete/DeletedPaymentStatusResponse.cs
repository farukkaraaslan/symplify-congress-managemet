namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Delete;
public class DeletedPaymentStatusResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
