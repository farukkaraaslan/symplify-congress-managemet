using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Cities.Commands.Update;
public class UpdateCityCommandValidator : AbstractValidator<UpdateCityCommand>
{
    public UpdateCityCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
