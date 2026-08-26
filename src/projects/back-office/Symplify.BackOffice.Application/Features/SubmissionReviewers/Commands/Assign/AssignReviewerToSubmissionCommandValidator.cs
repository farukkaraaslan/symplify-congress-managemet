using FluentValidation;

namespace Symplify.BackOffice.Application.Features.SubmissionReviewers.Commands.Assign;

public sealed class AssignReviewerToSubmissionCommandValidator : AbstractValidator<AssignReviewerToSubmissionCommand>
{
    public AssignReviewerToSubmissionCommandValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.ReviewerId).NotEmpty();
    }
}
