namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Update;

public class UpdatedTransactionStatusPhaseResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsActive { get; set; }
}
