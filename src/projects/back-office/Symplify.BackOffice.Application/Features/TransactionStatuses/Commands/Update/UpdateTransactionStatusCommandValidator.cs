using FluentValidation;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Update;

public class UpdateTransactionStatusCommandValidator : AbstractValidator<UpdateTransactionStatusCommand>
{
    public UpdateTransactionStatusCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage(TransactionStatusesMessages.EntityNotFound);

        RuleFor(command => command.TransactionStatusPhaseId)
            .GreaterThan(0)
            .WithMessage(TransactionStatusesMessages.PhaseRequired);

        RuleFor(command => command.Code)
            .NotEmpty()
            .WithMessage(TransactionStatusesMessages.CodeRequired)
            .MaximumLength(100)
            .WithMessage(TransactionStatusesMessages.CodeMaxLength);

        RuleFor(command => command.Translations)
            .NotEmpty()
            .WithMessage(TransactionStatusesMessages.DefaultTranslationRequired);
    }
}
