using FluentValidation;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Commands.Create;

public class CreateWorkflowTemplateCommandValidator : AbstractValidator<CreateWorkflowTemplateCommand>
{
    public CreateWorkflowTemplateCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage(WorkflowTemplatesMessages.CodeRequired)
            .MaximumLength(100).WithMessage(WorkflowTemplatesMessages.CodeMaxLength);

        RuleFor(command => command.Translations)
            .NotEmpty().WithMessage(WorkflowTemplatesMessages.DefaultTranslationRequired);
    }
}
