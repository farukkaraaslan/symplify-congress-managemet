using FluentValidation;
using Symplify.BackOffice.Application.Features.BulkEmails.Constants;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Commands.Queue;

public sealed class QueueBulkEmailCommandValidator : AbstractValidator<QueueBulkEmailCommand>
{
    private const int MaxRecipientAdjustments = 5000;

    public QueueBulkEmailCommandValidator()
    {
        RuleFor(command => command.CongressId)
            .NotEmpty()
            .WithMessage(BulkEmailsMessages.CongressRequired);

        RuleFor(command => command.AudienceType)
            .IsInEnum()
            .NotEqual(default(BulkEmailAudienceType))
            .WithMessage(BulkEmailsMessages.AudienceRequired);

        RuleFor(command => command.Subject)
            .Must(subject => !string.IsNullOrWhiteSpace(subject))
            .WithMessage(BulkEmailsMessages.SubjectRequired)
            .MaximumLength(200)
            .WithMessage(BulkEmailsMessages.SubjectTooLong)
            .Must(subject =>
                string.IsNullOrEmpty(subject) ||
                (!subject.Contains('\r') && !subject.Contains('\n')))
            .WithMessage(BulkEmailsMessages.SubjectInvalid);

        RuleFor(command => command.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage(BulkEmailsMessages.TitleRequired)
            .MaximumLength(200)
            .WithMessage(BulkEmailsMessages.TitleTooLong);

        RuleFor(command => command.BodyText)
            .Must(body => !string.IsNullOrWhiteSpace(body))
            .WithMessage(BulkEmailsMessages.BodyRequired)
            .MaximumLength(20000)
            .WithMessage(BulkEmailsMessages.BodyTooLong);

        RuleFor(command => command.ExcludedRecipientEmails)
            .Must(items => items.Count <= MaxRecipientAdjustments)
            .WithMessage(BulkEmailsMessages.RecipientSelectionInvalid);

        RuleForEach(command => command.ExcludedRecipientEmails)
            .MaximumLength(320)
            .WithMessage(BulkEmailsMessages.RecipientSelectionInvalid);

        RuleFor(command => command.AdditionalRecipients)
            .Must(items => items.Count <= MaxRecipientAdjustments)
            .WithMessage(BulkEmailsMessages.RecipientSelectionInvalid);

        RuleForEach(command => command.AdditionalRecipients)
            .ChildRules(recipient =>
            {
                recipient.RuleFor(item => item.Email)
                    .NotEmpty()
                    .MaximumLength(320)
                    .WithMessage(BulkEmailsMessages.RecipientSelectionInvalid);

                recipient.RuleFor(item => item.Name)
                    .MaximumLength(250)
                    .WithMessage(BulkEmailsMessages.RecipientSelectionInvalid);
            });

        RuleFor(command => command.TrackingBaseUrl)
            .NotEmpty()
            .Must(IsValidTrackingBaseUrl)
            .WithMessage(BulkEmailsMessages.TrackingBaseUrlInvalid);
    }

    private static bool IsValidTrackingBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
            return false;

        return string.IsNullOrWhiteSpace(uri.UserInfo) &&
               (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
    }
}
