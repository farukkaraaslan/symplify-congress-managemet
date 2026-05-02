using FluentValidation;
namespace Symplify.BackOffice.Application.Features.Languages.Commands.Update;
public class UpdateLanguageCommandValidator : AbstractValidator<UpdateLanguageCommand>
{
    public UpdateLanguageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
