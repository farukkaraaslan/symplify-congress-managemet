using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create;
public class CreateTransactionStatusTransitionCommandValidator : AbstractValidator<CreateTransactionStatusTransitionCommand>
{
    public CreateTransactionStatusTransitionCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
