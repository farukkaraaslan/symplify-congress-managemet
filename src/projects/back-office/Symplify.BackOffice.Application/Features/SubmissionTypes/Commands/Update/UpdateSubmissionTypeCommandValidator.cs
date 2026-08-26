using FluentValidation;
namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Update;
public class UpdateSubmissionTypeCommandValidator : AbstractValidator<UpdateSubmissionTypeCommand>
{
    public UpdateSubmissionTypeCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Translations).NotEmpty(); }
}
