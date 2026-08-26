using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;

namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Create;

public class CreateCongressPaymentPlanCommandValidator : AbstractValidator<CreateCongressPaymentPlanCommand>
{
    public CreateCongressPaymentPlanCommandValidator()
    {
        RuleFor(x => x.CongressId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.AudienceType).Must(CongressPaymentPlanAudienceTypes.IsValid);
        RuleFor(x => x.PaymentCategory).Must(CongressPaymentPlanCategories.IsValid);
        RuleFor(x => x.Translations).NotEmpty();
        RuleFor(x => x.ValidUntil)
            .GreaterThanOrEqualTo(x => x.ValidFrom)
            .When(x => x.ValidFrom.HasValue && x.ValidUntil.HasValue);
    }
}
