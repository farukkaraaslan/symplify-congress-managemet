using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Update;
public class UpdateCongressPaymentPlanCommandValidator : AbstractValidator<UpdateCongressPaymentPlanCommand>
{
    public UpdateCongressPaymentPlanCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
