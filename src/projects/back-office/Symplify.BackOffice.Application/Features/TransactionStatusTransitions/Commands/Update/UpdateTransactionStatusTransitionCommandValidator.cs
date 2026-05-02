using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Update;
public class UpdateTransactionStatusTransitionCommandValidator : AbstractValidator<UpdateTransactionStatusTransitionCommand>
{
    public UpdateTransactionStatusTransitionCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
