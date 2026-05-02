using FluentValidation;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Update;
public class UpdateEvaluationScoreCommandValidator : AbstractValidator<UpdateEvaluationScoreCommand>
{
    public UpdateEvaluationScoreCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
