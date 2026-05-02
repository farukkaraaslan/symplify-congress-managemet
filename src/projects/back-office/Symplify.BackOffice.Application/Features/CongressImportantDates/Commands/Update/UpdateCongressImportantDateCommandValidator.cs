using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Update;
public class UpdateCongressImportantDateCommandValidator : AbstractValidator<UpdateCongressImportantDateCommand>
{
    public UpdateCongressImportantDateCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
