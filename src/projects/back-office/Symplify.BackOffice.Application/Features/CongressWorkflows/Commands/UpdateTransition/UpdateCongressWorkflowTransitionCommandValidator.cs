using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.UpdateTransition;

public class UpdateCongressWorkflowTransitionCommandValidator : AbstractValidator<UpdateCongressWorkflowTransitionCommand>
{
    public UpdateCongressWorkflowTransitionCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage(CongressWorkflowsMessages.EntityNotFound);

        RuleFor(command => command.TransactionStatusTransitionId)
            .GreaterThan(0)
            .WithMessage(CongressWorkflowsMessages.TransitionNotFound);
    }
}
