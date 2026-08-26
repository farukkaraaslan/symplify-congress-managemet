using FluentValidation;

namespace Symplify.BackOffice.Application.Features.Users.Commands.Create;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Surname).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Institution).MaximumLength(250);
        RuleFor(command => command.Orcid).MaximumLength(100);
        RuleFor(command => command.PhoneNumber).MaximumLength(50);

        When(command => !command.GeneratePassword, () =>
        {
            RuleFor(command => command.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);
        });
    }
}
