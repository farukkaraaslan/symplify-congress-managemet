using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Create;
public class CreateCongressPaymentPlanCommandValidator : AbstractValidator<CreateCongressPaymentPlanCommand>
{
    public CreateCongressPaymentPlanCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
