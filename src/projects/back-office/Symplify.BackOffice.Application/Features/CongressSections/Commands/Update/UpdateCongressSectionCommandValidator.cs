using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Update;
public class UpdateCongressSectionCommandValidator : AbstractValidator<UpdateCongressSectionCommand>
{
    public UpdateCongressSectionCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
