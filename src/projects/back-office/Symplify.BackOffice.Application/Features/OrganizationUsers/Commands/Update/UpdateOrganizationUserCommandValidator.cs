using FluentValidation;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Update;
public class UpdateOrganizationUserCommandValidator : AbstractValidator<UpdateOrganizationUserCommand>
{
    public UpdateOrganizationUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
