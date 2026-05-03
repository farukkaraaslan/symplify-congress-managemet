using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.AddTransition;

public class AddCongressWorkflowTransitionCommandValidator : AbstractValidator<AddCongressWorkflowTransitionCommand>
{
    public AddCongressWorkflowTransitionCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressWorkflowsMessages.CongressNotFound);

        RuleFor(command => command.TransactionStatusTransitionId)
            .GreaterThan(0)
            .WithMessage(CongressWorkflowsMessages.TransitionNotFound);
    }
}
