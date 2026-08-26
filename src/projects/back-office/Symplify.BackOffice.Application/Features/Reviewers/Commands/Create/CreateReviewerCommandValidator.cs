using FluentValidation;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;

namespace Symplify.BackOffice.Application.Features.Reviewers.Commands.Create;

public class CreateReviewerCommandValidator : AbstractValidator<CreateReviewerCommand>
{
    public CreateReviewerCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(ReviewersMessages.UserIdRequired);

        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
