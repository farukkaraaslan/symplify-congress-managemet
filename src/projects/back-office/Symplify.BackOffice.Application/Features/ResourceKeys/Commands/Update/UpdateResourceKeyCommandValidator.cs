using FluentValidation;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Update;
public class UpdateResourceKeyCommandValidator : AbstractValidator<UpdateResourceKeyCommand>
{
    public UpdateResourceKeyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
