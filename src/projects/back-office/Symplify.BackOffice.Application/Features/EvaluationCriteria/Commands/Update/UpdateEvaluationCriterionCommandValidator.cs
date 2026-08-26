using FluentValidation;

namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Update;

public class UpdateEvaluationCriterionCommandValidator : AbstractValidator<UpdateEvaluationCriterionCommand>
{
    public UpdateEvaluationCriterionCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Score).InclusiveBetween(1, 100);
        RuleFor(command => command.Translations).NotEmpty();
    }
}
