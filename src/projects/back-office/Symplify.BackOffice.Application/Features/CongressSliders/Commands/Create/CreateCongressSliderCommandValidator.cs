using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Create;
public class CreateCongressSliderCommandValidator : AbstractValidator<CreateCongressSliderCommand>
{
    public CreateCongressSliderCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
