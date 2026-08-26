using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Regions.Commands.Create;
public class CreateRegionCommandValidator : AbstractValidator<CreateRegionCommand>
{
    public CreateRegionCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
