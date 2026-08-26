using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;

public class CreateCongressBoardMemberCommandValidator : AbstractValidator<CreateCongressBoardMemberCommand>
{
    public CreateCongressBoardMemberCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(CongressBoardMembersMessages.CongressRequired);

        RuleFor(command => command.FullName)
            .NotEmpty()
            .WithMessage(CongressBoardMembersMessages.FullNameRequired)
            .MaximumLength(250)
            .WithMessage(CongressBoardMembersMessages.FullNameMaxLength);

        RuleFor(command => command.AcademicTitle)
            .MaximumLength(100)
            .WithMessage(CongressBoardMembersMessages.AcademicTitleMaxLength);

        RuleFor(command => command.Institution)
            .MaximumLength(500)
            .WithMessage(CongressBoardMembersMessages.InstitutionMaxLength);

        RuleFor(command => command.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage(CongressBoardMembersMessages.OrderMustBePositive);
    }
}
