using FluentValidation;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Update;
public class UpdatePaymentDocumentCommandValidator : AbstractValidator<UpdatePaymentDocumentCommand>
{
    public UpdatePaymentDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
