namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Update;

public class UpdatedTransactionStatusResponse
{
    public int Id { get; set; }

    public int TransactionStatusPhaseId { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsEditable { get; set; }

    public bool IsFinal { get; set; }

    public bool IsActive { get; set; }
}
