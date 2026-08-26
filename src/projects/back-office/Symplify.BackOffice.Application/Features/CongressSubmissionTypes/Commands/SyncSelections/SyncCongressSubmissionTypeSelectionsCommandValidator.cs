using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;

namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.SyncSelections;

public sealed class SyncCongressSubmissionTypeSelectionsCommandValidator : AbstractValidator<SyncCongressSubmissionTypeSelectionsCommand>
{
    public SyncCongressSubmissionTypeSelectionsCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty();

        RuleFor(command => command.SelectedSubmissionTypeIds)
            .NotNull();

        RuleForEach(command => command.SelectedSubmissionTypeIds)
            .NotEmpty()
            .WithMessage(CongressSubmissionTypesMessages.InvalidSelectionId);

        RuleFor(command => command.SelectedSubmissionTypeIds)
            .Must(ids => ids == null || ids.Count == ids.Distinct().Count())
            .WithMessage(CongressSubmissionTypesMessages.DuplicateSelectionId);
    }
}
