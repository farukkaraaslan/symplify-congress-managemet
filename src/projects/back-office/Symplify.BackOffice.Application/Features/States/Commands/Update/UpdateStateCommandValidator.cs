using FluentValidation;
namespace Symplify.BackOffice.Application.Features.States.Commands.Update;
public class UpdateStateCommandValidator : AbstractValidator<UpdateStateCommand>
{
    public UpdateStateCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
