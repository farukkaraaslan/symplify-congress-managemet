using FluentValidation;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.Reject;

public sealed class RejectSubmissionCommandValidator : AbstractValidator<RejectSubmissionCommand>
{
    public RejectSubmissionCommandValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.PublicNote).MaximumLength(2000);
        RuleFor(command => command.InternalNote).MaximumLength(2000);
    }
}
