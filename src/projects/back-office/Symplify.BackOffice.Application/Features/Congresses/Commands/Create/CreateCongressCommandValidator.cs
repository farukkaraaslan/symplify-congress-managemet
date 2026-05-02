using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Create;
public class CreateCongressCommandValidator : AbstractValidator<CreateCongressCommand>
{
    public CreateCongressCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
