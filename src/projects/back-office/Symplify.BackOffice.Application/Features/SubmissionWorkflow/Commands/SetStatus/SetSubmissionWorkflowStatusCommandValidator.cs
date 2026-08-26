using FluentValidation;
using Symplify.BackOffice.Application.Features.SubmissionWorkflow.Constants;
using Symplify.BackOffice.Application.Services.Workflow;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.SetStatus;

public sealed class SetSubmissionWorkflowStatusCommandValidator : AbstractValidator<SetSubmissionWorkflowStatusCommand>
{
    public SetSubmissionWorkflowStatusCommandValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.TargetStatusCode)
            .NotEmpty()
            .Must(BeSupportedTargetStatus)
            .WithMessage(SubmissionWorkflowMessages.UnsupportedTargetStatus);
        RuleFor(command => command.PublicNote).MaximumLength(2000);
        RuleFor(command => command.InternalNote).MaximumLength(2000);
    }

    private static bool BeSupportedTargetStatus(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return false;

        return string.Equals(statusCode, SubmissionWorkflowStatusCodes.Rejected, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(statusCode, SubmissionWorkflowStatusCodes.Submitted, StringComparison.OrdinalIgnoreCase);
    }
}
