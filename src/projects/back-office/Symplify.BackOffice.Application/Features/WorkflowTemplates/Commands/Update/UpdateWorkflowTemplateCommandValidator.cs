using FluentValidation;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Commands.Update;

public class UpdateWorkflowTemplateCommandValidator : AbstractValidator<UpdateWorkflowTemplateCommand>
{
    public UpdateWorkflowTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage(WorkflowTemplatesMessages.EntityNotFound);
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage(WorkflowTemplatesMessages.CodeRequired)
            .MaximumLength(100).WithMessage(WorkflowTemplatesMessages.CodeMaxLength);
        RuleFor(command => command.Translations)
            .NotEmpty().WithMessage(WorkflowTemplatesMessages.DefaultTranslationRequired);
    }
}
