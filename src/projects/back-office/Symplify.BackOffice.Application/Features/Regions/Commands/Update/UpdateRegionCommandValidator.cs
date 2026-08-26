using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Regions.Commands.Update;
public class UpdateRegionCommandValidator : AbstractValidator<UpdateRegionCommand>
{
    public UpdateRegionCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
