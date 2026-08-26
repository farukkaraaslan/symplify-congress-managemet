using FluentValidation;

namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Commands.Save;

public sealed class SaveReviewerEvaluationCommandValidator : AbstractValidator<SaveReviewerEvaluationCommand>
{
    public SaveReviewerEvaluationCommandValidator()
    {
        RuleFor(command => command.EvaluationId).NotEmpty();
        RuleForEach(command => command.Scores).ChildRules(score =>
        {
            score.RuleFor(item => item.EvaluationCriterionId).NotEmpty();
            score.RuleFor(item => item.Score)
                .InclusiveBetween(0, 100)
                .When(item => item.Score.HasValue);
            score.RuleFor(item => item.Comment).MaximumLength(1000);
        });
        RuleFor(command => command.Comment).MaximumLength(4000);
        RuleFor(command => command.EditorComment).MaximumLength(4000);
        RuleFor(command => command.Recommendation).MaximumLength(200);
    }
}
