using FluentValidation;

namespace Symplify.BackOffice.Application.Features.Users.Commands.Update;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Surname).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Institution).MaximumLength(250);
        RuleFor(command => command.Orcid).MaximumLength(100);
        RuleFor(command => command.PhoneNumber).MaximumLength(50);
        RuleFor(command => command.TitleId).Must(id => !id.HasValue || id.Value != Guid.Empty);
        RuleFor(command => command.CountryId).Must(id => !id.HasValue || id.Value != Guid.Empty);
        RuleFor(command => command.StateId).Must(id => !id.HasValue || id.Value != Guid.Empty);
        RuleFor(command => command.OrganizationId).Must(id => !id.HasValue || id.Value != Guid.Empty);
        RuleFor(command => command.DefaultCongressId).Must(id => !id.HasValue || id.Value != Guid.Empty);
    }
}
