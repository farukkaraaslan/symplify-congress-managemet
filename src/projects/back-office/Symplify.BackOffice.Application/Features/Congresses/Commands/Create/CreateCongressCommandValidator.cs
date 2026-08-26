using FluentValidation;

namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Create;

public sealed class CreateCongressCommandValidator : AbstractValidator<CreateCongressCommand>
{
    public CreateCongressCommandValidator()
    {
        RuleFor(command => command.OrganizationId)
            .NotEmpty();

        RuleFor(command => command.StartDate)
            .NotNull();

        RuleFor(command => command.EndDate)
            .NotNull();

        RuleFor(command => command.ContactPhone)
            .MaximumLength(64);

        RuleFor(command => command.ContactName)
            .MaximumLength(200);

        RuleFor(command => command.ContactTitle)
            .MaximumLength(200);

        RuleFor(command => command.ContactAddress)
            .MaximumLength(1000);

        RuleFor(command => command.VenueName)
            .MaximumLength(250);

        RuleFor(command => command.EditionNumber)
            .GreaterThan(0)
            .When(command => command.EditionNumber.HasValue);

        RuleFor(command => command.Translations)
            .NotEmpty();

        RuleForEach(command => command.ContactEmails)
            .ChildRules(email =>
            {
                email.RuleFor(item => item.Email)
                    .NotEmpty()
                    .EmailAddress()
                    .MaximumLength(256);

                email.RuleFor(item => item.Label)
                    .MaximumLength(100);
            });

        RuleFor(command => command.ContactEmails)
            .Must(items => items.Count(item => item.IsPrimary) <= 1)
            .WithMessage("Yalnızca bir iletişim e-posta adresi ana adres olarak seçilebilir.");
    }
}
