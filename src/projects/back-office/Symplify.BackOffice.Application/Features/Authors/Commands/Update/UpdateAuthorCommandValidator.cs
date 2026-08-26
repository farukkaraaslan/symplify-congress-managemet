using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Authors.Commands.Update;
public class UpdateAuthorCommandValidator : AbstractValidator<UpdateAuthorCommand>
{
    public UpdateAuthorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
