using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Update;
public class UpdateCongressBoardMemberCommandValidator : AbstractValidator<UpdateCongressBoardMemberCommand>
{
    public UpdateCongressBoardMemberCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
