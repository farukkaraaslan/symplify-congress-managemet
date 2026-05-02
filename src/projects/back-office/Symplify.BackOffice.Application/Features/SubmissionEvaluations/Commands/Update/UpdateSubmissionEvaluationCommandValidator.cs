using FluentValidation;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Update;
public class UpdateSubmissionEvaluationCommandValidator : AbstractValidator<UpdateSubmissionEvaluationCommand>
{
    public UpdateSubmissionEvaluationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
