using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Commands.Update;
public class UpdateTenantUserCommandValidator : AbstractValidator<UpdateTenantUserCommand>
{
    public UpdateTenantUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
