namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Create;
public class CreatedTransactionStatusResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
