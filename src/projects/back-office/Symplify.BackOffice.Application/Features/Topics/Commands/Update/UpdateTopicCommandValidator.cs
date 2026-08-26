using FluentValidation;
using Symplify.BackOffice.Application.Features.Topics.Constants;

namespace Symplify.BackOffice.Application.Features.Topics.Commands.Update;

public class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
{
    public UpdateTopicCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage(TopicResourceKeys.Messages.InvalidTopic);

        RuleFor(command => command.Translations)
            .NotEmpty()
            .WithMessage(TopicResourceKeys.Validation.AtLeastOneTranslationRequired);

        RuleFor(command => command.Translations)
            .Must(translations => translations.Any(translation =>
                translation.Fields.TryGetValue("Name", out string? name) &&
                !string.IsNullOrWhiteSpace(name)))
            .WithMessage(TopicResourceKeys.Validation.AtLeastOneTranslationRequired);

        RuleForEach(command => command.Translations)
            .ChildRules(translation =>
            {
                translation.RuleFor(item => item.LanguageId)
                    .NotEmpty()
                    .WithMessage(TopicResourceKeys.Validation.LanguageRequired);

                translation.RuleFor(item => item.Fields)
                    .Must(fields => !fields.TryGetValue("Name", out string? name) || string.IsNullOrWhiteSpace(name) || name.Length <= 250)
                    .WithMessage(TopicResourceKeys.Validation.NameMaxLength);

                translation.RuleFor(item => item.Fields)
                    .Must(fields => !fields.TryGetValue("Description", out string? description) || string.IsNullOrWhiteSpace(description) || description.Length <= 1000)
                    .WithMessage(TopicResourceKeys.Validation.DescriptionMaxLength);
            });
    }
}
