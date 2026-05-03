namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Create;

public class CreatedTransactionStatusPhaseResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsActive { get; set; }
}
