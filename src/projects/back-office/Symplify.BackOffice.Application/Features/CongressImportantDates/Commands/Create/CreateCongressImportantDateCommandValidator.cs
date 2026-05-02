using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Create;
public class CreateCongressImportantDateCommandValidator : AbstractValidator<CreateCongressImportantDateCommand>
{
    public CreateCongressImportantDateCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
