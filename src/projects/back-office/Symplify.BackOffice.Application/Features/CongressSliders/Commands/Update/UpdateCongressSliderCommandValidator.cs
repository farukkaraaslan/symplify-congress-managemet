using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Update;
public class UpdateCongressSliderCommandValidator : AbstractValidator<UpdateCongressSliderCommand>
{
    public UpdateCongressSliderCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
