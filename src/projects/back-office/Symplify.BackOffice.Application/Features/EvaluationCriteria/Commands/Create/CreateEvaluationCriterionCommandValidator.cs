using FluentValidation;
namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Create;
public class CreateEvaluationCriterionCommandValidator : AbstractValidator<CreateEvaluationCriterionCommand>
{
    public CreateEvaluationCriterionCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
