using FluentValidation;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Update;
public class UpdateCongressSubmissionTypeCommandValidator : AbstractValidator<UpdateCongressSubmissionTypeCommand>
{
    public UpdateCongressSubmissionTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
