using FluentValidation;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.RestartRejectedProcess;

public sealed class RestartRejectedSubmissionProcessCommandValidator : AbstractValidator<RestartRejectedSubmissionProcessCommand>
{
    public RestartRejectedSubmissionProcessCommandValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.PublicNote).MaximumLength(2000);
        RuleFor(command => command.InternalNote).MaximumLength(2000);
    }
}
