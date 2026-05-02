using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Countries.Commands.Create;
public class CreateCountryCommandValidator : AbstractValidator<CreateCountryCommand>
{
    public CreateCountryCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
