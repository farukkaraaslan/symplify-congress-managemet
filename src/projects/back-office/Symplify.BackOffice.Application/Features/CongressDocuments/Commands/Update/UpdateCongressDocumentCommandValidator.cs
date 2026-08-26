using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Update;

public class UpdateCongressDocumentCommandValidator : AbstractValidator<UpdateCongressDocumentCommand>
{
    public UpdateCongressDocumentCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage(CongressDocumentsMessages.EntityNotFound);

        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressDocumentsMessages.CongressRequired);

        RuleFor(command => command.DocumentTypeId)
            .NotEmpty()
            .WithMessage(CongressDocumentsMessages.DocumentTypeRequired);
    }
}
