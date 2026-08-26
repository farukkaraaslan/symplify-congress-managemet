using FluentValidation;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;

namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.SyncSelections;

public sealed class SyncCongressTopicSelectionsCommandValidator : AbstractValidator<SyncCongressTopicSelectionsCommand>
{
    public SyncCongressTopicSelectionsCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty();

        RuleFor(command => command.SelectedTopicIds)
            .NotNull();

        RuleForEach(command => command.SelectedTopicIds)
            .NotEmpty()
            .WithMessage(CongressTopicsMessages.InvalidSelectionId);

        RuleFor(command => command.SelectedTopicIds)
            .Must(ids => ids == null || ids.Count == ids.Distinct().Count())
            .WithMessage(CongressTopicsMessages.DuplicateSelectionId);
    }
}
