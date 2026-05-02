using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Update;
public class UpdateCongressDocumentCommandValidator : AbstractValidator<UpdateCongressDocumentCommand>
{
    public UpdateCongressDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
