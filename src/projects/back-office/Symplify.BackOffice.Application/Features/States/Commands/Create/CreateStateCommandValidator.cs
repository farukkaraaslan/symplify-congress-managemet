using FluentValidation;
namespace Symplify.BackOffice.Application.Features.States.Commands.Create;
public class CreateStateCommandValidator : AbstractValidator<CreateStateCommand>
{
    public CreateStateCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
