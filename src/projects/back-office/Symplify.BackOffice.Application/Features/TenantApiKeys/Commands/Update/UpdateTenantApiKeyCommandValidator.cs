using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Update;
public class UpdateTenantApiKeyCommandValidator : AbstractValidator<UpdateTenantApiKeyCommand>
{
    public UpdateTenantApiKeyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
