using FluentValidation;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Create;

public class CreateTransactionStatusPhaseCommandValidator : AbstractValidator<CreateTransactionStatusPhaseCommand>
{
    public CreateTransactionStatusPhaseCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .WithMessage(TransactionStatusPhasesMessages.CodeRequired)
            .MaximumLength(100)
            .WithMessage(TransactionStatusPhasesMessages.CodeMaxLength);

        RuleFor(command => command.Translations)
            .NotEmpty()
            .WithMessage(TransactionStatusPhasesMessages.DefaultTranslationRequired);
    }
}
