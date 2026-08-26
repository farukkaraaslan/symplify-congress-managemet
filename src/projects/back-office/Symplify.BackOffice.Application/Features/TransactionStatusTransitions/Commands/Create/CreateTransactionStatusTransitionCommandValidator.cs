using FluentValidation;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;

namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create;

public class CreateTransactionStatusTransitionCommandValidator : AbstractValidator<CreateTransactionStatusTransitionCommand>
{
    public CreateTransactionStatusTransitionCommandValidator()
    {
        RuleFor(command => command.FromStatusId)
            .GreaterThan(0)
            .WithMessage(TransactionStatusTransitionsMessages.FromStatusRequired);

        RuleFor(command => command.ToStatusId)
            .GreaterThan(0)
            .WithMessage(TransactionStatusTransitionsMessages.ToStatusRequired);

        RuleFor(command => command)
            .Must(command => command.FromStatusId != command.ToStatusId)
            .WithMessage(TransactionStatusTransitionsMessages.FromAndToStatusCannotBeSame);

        RuleFor(command => command.Translations)
            .NotEmpty()
            .WithMessage(TransactionStatusTransitionsMessages.DefaultTranslationRequired);
    }
}
