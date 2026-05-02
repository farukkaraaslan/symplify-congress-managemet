using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Tenants.Commands.Update;
public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
