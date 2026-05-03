using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.ApplyTemplate;

public class ApplyWorkflowTemplateToCongressCommandValidator : AbstractValidator<ApplyWorkflowTemplateToCongressCommand>
{
    public ApplyWorkflowTemplateToCongressCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressWorkflowsMessages.CongressNotFound);

        RuleFor(command => command.WorkflowTemplateId)
            .NotEmpty()
            .WithMessage(CongressWorkflowsMessages.TemplateNotFound);
    }
}
