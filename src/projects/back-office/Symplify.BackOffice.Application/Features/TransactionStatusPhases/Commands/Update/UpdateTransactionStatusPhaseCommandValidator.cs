using FluentValidation;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Update;

public class UpdateTransactionStatusPhaseCommandValidator : AbstractValidator<UpdateTransactionStatusPhaseCommand>
{
    public UpdateTransactionStatusPhaseCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage(TransactionStatusPhasesMessages.EntityNotFound);

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
