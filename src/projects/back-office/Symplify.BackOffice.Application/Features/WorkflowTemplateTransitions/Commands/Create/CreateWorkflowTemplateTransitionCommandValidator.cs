using FluentValidation;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Create;

public class CreateWorkflowTemplateTransitionCommandValidator : AbstractValidator<CreateWorkflowTemplateTransitionCommand>
{
    public CreateWorkflowTemplateTransitionCommandValidator()
    {
        RuleFor(command => command.WorkflowTemplateId)
            .NotEmpty()
            .WithMessage(WorkflowTemplateTransitionsMessages.TemplateNotFound);

        RuleFor(command => command.TransactionStatusTransitionId)
            .GreaterThan(0)
            .WithMessage(WorkflowTemplateTransitionsMessages.TransitionNotFound);
    }
}
