using FluentValidation;
namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Create;
public class CreateSubmissionTypeCommandValidator : AbstractValidator<CreateSubmissionTypeCommand>
{
    public CreateSubmissionTypeCommandValidator() { RuleFor(x => x.Translations).NotEmpty(); }
}
