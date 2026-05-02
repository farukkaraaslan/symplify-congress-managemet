using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Update;
public class UpdateTransactionStatusCommandValidator : AbstractValidator<UpdateTransactionStatusCommand>
{
    public UpdateTransactionStatusCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
