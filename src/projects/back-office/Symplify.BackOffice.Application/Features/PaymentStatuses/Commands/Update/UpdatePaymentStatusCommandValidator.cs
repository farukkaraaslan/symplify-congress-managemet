using FluentValidation;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Update;
public class UpdatePaymentStatusCommandValidator : AbstractValidator<UpdatePaymentStatusCommand>
{
    public UpdatePaymentStatusCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
