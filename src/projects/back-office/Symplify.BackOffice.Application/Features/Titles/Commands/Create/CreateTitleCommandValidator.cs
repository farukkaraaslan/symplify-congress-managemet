using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Titles.Commands.Create;
public class CreateTitleCommandValidator : AbstractValidator<CreateTitleCommand>
{
    public CreateTitleCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
