using FluentValidation;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.ChangeStatus;

public sealed class ChangeSubmissionStatusCommandValidator : AbstractValidator<ChangeSubmissionStatusCommand>
{
    public ChangeSubmissionStatusCommandValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.TransitionId).GreaterThan(0);
        RuleFor(command => command.PublicNote).MaximumLength(2000);
        RuleFor(command => command.InternalNote).MaximumLength(2000);
    }
}
