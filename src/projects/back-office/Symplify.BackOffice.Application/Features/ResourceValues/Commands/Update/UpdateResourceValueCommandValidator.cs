using FluentValidation;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Commands.Update;
public class UpdateResourceValueCommandValidator : AbstractValidator<UpdateResourceValueCommand>
{
    public UpdateResourceValueCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
