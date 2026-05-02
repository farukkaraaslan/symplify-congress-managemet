using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Reviewers.Commands.Update;
public class UpdateReviewerCommandValidator : AbstractValidator<UpdateReviewerCommand>
{
    public UpdateReviewerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
