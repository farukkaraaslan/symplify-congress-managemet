using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Authors.Commands.Create;
public class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
{
    public CreateAuthorCommandValidator()
    {
    }
}
