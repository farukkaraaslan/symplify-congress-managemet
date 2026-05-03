using FluentValidation;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Update;

public class UpdateWorkflowTemplateTransitionCommandValidator : AbstractValidator<UpdateWorkflowTemplateTransitionCommand>
{
    public UpdateWorkflowTemplateTransitionCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage(WorkflowTemplateTransitionsMessages.EntityNotFound);

        RuleFor(command => command.TransactionStatusTransitionId)
            .GreaterThan(0)
            .WithMessage(WorkflowTemplateTransitionsMessages.TransitionNotFound);
    }
}
