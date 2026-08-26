using FluentValidation;

namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Create;

public class CreateEvaluationCriterionCommandValidator : AbstractValidator<CreateEvaluationCriterionCommand>
{
    public CreateEvaluationCriterionCommandValidator()
    {
        RuleFor(command => command.Score).InclusiveBetween(1, 100);
        RuleFor(command => command.Translations).NotEmpty();
    }
}
