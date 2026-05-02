using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Create;
public class CreateCongressBoardCommandValidator : AbstractValidator<CreateCongressBoardCommand>
{
    public CreateCongressBoardCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
