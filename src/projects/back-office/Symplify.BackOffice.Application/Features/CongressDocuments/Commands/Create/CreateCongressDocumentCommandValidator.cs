using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Create;

public class CreateCongressDocumentCommandValidator : AbstractValidator<CreateCongressDocumentCommand>
{
    public CreateCongressDocumentCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressDocumentsMessages.CongressRequired);

        RuleFor(command => command.DocumentTypeId)
            .NotEmpty()
            .WithMessage(CongressDocumentsMessages.DocumentTypeRequired);
    }
}
