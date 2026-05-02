namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Delete;
public class DeletedTransactionStatusResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
