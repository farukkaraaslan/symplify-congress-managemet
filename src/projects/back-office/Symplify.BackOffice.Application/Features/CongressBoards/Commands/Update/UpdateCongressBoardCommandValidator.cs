using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Update;
public class UpdateCongressBoardCommandValidator : AbstractValidator<UpdateCongressBoardCommand>
{
    public UpdateCongressBoardCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
