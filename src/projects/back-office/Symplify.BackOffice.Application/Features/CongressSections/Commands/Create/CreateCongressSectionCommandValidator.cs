using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Create;
public class CreateCongressSectionCommandValidator : AbstractValidator<CreateCongressSectionCommand>
{
    public CreateCongressSectionCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
