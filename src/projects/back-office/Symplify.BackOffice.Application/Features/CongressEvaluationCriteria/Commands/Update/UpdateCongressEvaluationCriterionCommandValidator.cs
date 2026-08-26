using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Update;
public class UpdateCongressEvaluationCriterionCommandValidator : AbstractValidator<UpdateCongressEvaluationCriterionCommand>
{
    public UpdateCongressEvaluationCriterionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
