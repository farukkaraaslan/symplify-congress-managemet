using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Commands.Create;
public class CreateTenantUserCommandValidator : AbstractValidator<CreateTenantUserCommand>
{
    public CreateTenantUserCommandValidator()
    {
    }
}
