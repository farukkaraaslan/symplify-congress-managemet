using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Create;
public class CreateTransactionStatusCommandValidator : AbstractValidator<CreateTransactionStatusCommand>
{
    public CreateTransactionStatusCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
