using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Update;
public class UpdateCongressCommandValidator : AbstractValidator<UpdateCongressCommand>
{
    public UpdateCongressCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
