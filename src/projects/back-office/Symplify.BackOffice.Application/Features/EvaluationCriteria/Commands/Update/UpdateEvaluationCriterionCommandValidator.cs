using FluentValidation;
namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Update;
public class UpdateEvaluationCriterionCommandValidator : AbstractValidator<UpdateEvaluationCriterionCommand>
{
    public UpdateEvaluationCriterionCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
