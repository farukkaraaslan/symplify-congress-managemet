using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.Update;
public class UpdateCongressTopicCommandValidator : AbstractValidator<UpdateCongressTopicCommand>
{
    public UpdateCongressTopicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
