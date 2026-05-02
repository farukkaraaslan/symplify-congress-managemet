using FluentValidation;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Update;
public class UpdateSubmissionHistoryCommandValidator : AbstractValidator<UpdateSubmissionHistoryCommand>
{
    public UpdateSubmissionHistoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
