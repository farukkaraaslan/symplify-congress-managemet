using FluentValidation;
namespace Symplify.BackOffice.Application.Features.DocumentTypes.Commands.Create;
public class CreateDocumentTypeCommandValidator : AbstractValidator<CreateDocumentTypeCommand>
{
    public CreateDocumentTypeCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
