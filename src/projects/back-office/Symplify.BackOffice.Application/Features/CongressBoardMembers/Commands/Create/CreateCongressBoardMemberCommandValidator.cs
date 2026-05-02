using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;
public class CreateCongressBoardMemberCommandValidator : AbstractValidator<CreateCongressBoardMemberCommand>
{
    public CreateCongressBoardMemberCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
