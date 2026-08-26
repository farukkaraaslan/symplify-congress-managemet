namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create;

public class CreatedTransactionStatusTransitionResponse
{
    public int Id { get; set; }

    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public bool IsActive { get; set; }
}
