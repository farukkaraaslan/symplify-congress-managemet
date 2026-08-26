namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Update;

public class UpdatedTransactionStatusTransitionResponse
{
    public int Id { get; set; }

    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public bool IsActive { get; set; }
}
