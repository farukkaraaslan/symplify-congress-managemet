namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Delete;

public class DeletedTransactionStatusTransitionResponse
{
    public int Id { get; set; }

    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public bool IsActive { get; set; }
}
