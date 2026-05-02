using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Update;
public class UpdateSubmissionCommandValidator : AbstractValidator<UpdateSubmissionCommand>
{
    public UpdateSubmissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
