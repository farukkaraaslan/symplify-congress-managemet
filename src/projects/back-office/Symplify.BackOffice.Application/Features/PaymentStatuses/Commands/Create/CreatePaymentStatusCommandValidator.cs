using FluentValidation;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Create;
public class CreatePaymentStatusCommandValidator : AbstractValidator<CreatePaymentStatusCommand>
{
    public CreatePaymentStatusCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
