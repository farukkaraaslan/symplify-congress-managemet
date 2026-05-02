using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Tenants.Commands.Create;
public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
    }
}
