using FluentValidation;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Create;
public class CreatePaymentDocumentCommandValidator : AbstractValidator<CreatePaymentDocumentCommand>
{
    public CreatePaymentDocumentCommandValidator()
    {
    }
}
