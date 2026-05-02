using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Cities.Commands.Create;
public class CreateCityCommandValidator : AbstractValidator<CreateCityCommand>
{
    public CreateCityCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
