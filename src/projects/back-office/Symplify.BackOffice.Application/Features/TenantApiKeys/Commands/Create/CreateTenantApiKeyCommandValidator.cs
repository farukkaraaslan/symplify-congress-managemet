using FluentValidation;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Create;
public class CreateTenantApiKeyCommandValidator : AbstractValidator<CreateTenantApiKeyCommand>
{
    public CreateTenantApiKeyCommandValidator()
    {
    }
}
