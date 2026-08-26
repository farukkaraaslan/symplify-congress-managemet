using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class MailOutboxService : IMailOutboxService
{
    private const string SentToReviewTemplateCode = "SUBMISSION_SENT_TO_REVIEW";
    private const string PaymentPendingTemplateCode = "SUBMISSION_PAYMENT_PENDING";
    private const string PaymentApprovedTemplateCode = "SUBMISSION_PAYMENT_APPROVED";
    private const string AcceptedTemplateCode = "SUBMISSION_ACCEPTED";

    private readonly IMailOutboxMessageRepository _mailOutboxMessageRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISystemMailTemplateRenderer _mailTemplateRenderer;
    private readonly IMailBrandingResolver _mailBrandingResolver;
    private readonly IOrganizationMailConfigurationResolver _mailConfigurationResolver;
    private readonly IPublicUrlService _publicUrlService;

    public MailOutboxService(
        IMailOutboxMessageRepository mailOutboxMessageRepository,
        ISubmissionRepository submissionRepository,
        ISystemMailTemplateRenderer mailTemplateRenderer,
        IMailBrandingResolver mailBrandingResolver,
        IOrganizationMailConfigurationResolver mailConfigurationResolver,
        IPublicUrlService publicUrlService)
    {
        _mailOutboxMessageRepository = mailOutboxMessageRepository;
        _submissionRepository = submissionRepository;
        _mailTemplateRenderer = mailTemplateRenderer;
        _mailBrandingResolver = mailBrandingResolver;
        _mailConfigurationResolver = mailConfigurationResolver;
        _publicUrlService = publicUrlService;
    }

    public async Task QueueAcceptanceEmailAsync(
        Submission submission,
        SubmissionAcceptanceLetter acceptanceLetter,
        string? toEmail,
        CancellationToken cancellationToken)
    {
        string? recipient = !string.IsNullOrWhiteSpace(acceptanceLetter.AuthorEmailSnapshot)
            ? acceptanceLetter.AuthorEmailSnapshot
            : toEmail;

        if (string.IsNullOrWhiteSpace(recipient))
            return;

        Submission mailSubmission = await LoadSubmissionForMailAsync(submission, cancellationToken);
        string culture = ResolveCulture(mailSubmission);
        string actionUrl = BuildAcceptanceLetterPublicUrl(acceptanceLetter);
        string recipientName = ResolveAcceptanceRecipientName(mailSubmission, acceptanceLetter, recipient);
        MailBrandingModel branding = await _mailBrandingResolver.ResolveForSubmissionAsync(mailSubmission, cancellationToken);

        RenderedSystemMailTemplate rendered = await _mailTemplateRenderer.RenderAsync(
            new SystemMailTemplateRenderRequest
            {
                LanguageId = mailSubmission.LanguageId,
                Culture = culture,
                SubjectKey = SystemMailResourceKeys.SubmissionAcceptedSubject,
                TitleKey = SystemMailResourceKeys.SubmissionAcceptedTitle,
                BodyKey = SystemMailResourceKeys.SubmissionAcceptedBody,
                ActionTextKey = SystemMailResourceKeys.SubmissionAcceptedButton,
                ActionUrl = actionUrl,
                Branding = branding,
                Tokens = new Dictionary<string, string?>
                {
                    ["RecipientName"] = recipientName,
                    ["SubmissionNumber"] = NormalizeSubmissionNumber(mailSubmission),
                    ["SubmissionTitle"] = mailSubmission.Title,
                    ["CongressTitle"] = branding.ContextTitle,
                    ["LetterNumber"] = acceptanceLetter.LetterNumber,
                    ["FileName"] = acceptanceLetter.FileName,
                    ["AcceptanceLetterUrl"] = actionUrl
                },
                InfoRows = new List<MailInfoRowModel>
                {
                    new() { Label = Label(culture, "Bildiri No", "Submission Code"), Value = NormalizeSubmissionNumber(mailSubmission) },
                    new() { Label = Label(culture, "Bildiri Başlığı", "Submission Title"), Value = mailSubmission.Title },
                    new() { Label = Label(culture, "Kabul Belgesi", "Acceptance Letter"), Value = acceptanceLetter.FileName }
                },
                ShowIfNotRequestedMessage = false
            },
            cancellationToken);

        MailOutboxMessage message = new()
        {
            Id = Guid.NewGuid(),
            MailType = MailMessageType.AcceptanceLetter,
            RelatedUserId = ResolveRelatedUserId(mailSubmission, recipient),
            RelatedAuthorId = acceptanceLetter.AuthorId ?? ResolveRelatedAuthorId(mailSubmission, recipient),
            RelatedSubmissionId = mailSubmission.Id,
            AcceptanceLetterId = acceptanceLetter.Id,
            CongressId = mailSubmission.CongressId,
            ToEmail = recipient.Trim(),
            ToName = recipientName,
            Subject = rendered.Subject,
            HtmlBody = rendered.HtmlBody,
            AttachmentPath = null,
            AttachmentBucketName = null,
            AttachmentObjectName = null,
            AttachmentFileName = null,
            AttachmentContentType = null
        };

        await ApplySenderSnapshotAsync(message, mailSubmission, cancellationToken);
        await QueueIfNotExistsAsync(message, cancellationToken);
    }

    public Task QueuePaymentPendingEmailAsync(
        Submission submission,
        string? toEmail,
        CancellationToken cancellationToken)
    {
        return QueueSubmissionStatusEmailAsync(
            submission,
            PaymentPendingTemplateCode,
            toEmail,
            cancellationToken);
    }

    public Task QueuePaymentApprovedEmailAsync(
        Submission submission,
        string? toEmail,
        CancellationToken cancellationToken)
    {
        return QueueSubmissionStatusEmailAsync(
            submission,
            PaymentApprovedTemplateCode,
            toEmail,
            cancellationToken);
    }

    public async Task QueueSubmissionStatusEmailAsync(
        Submission submission,
        string templateCode,
        string? toEmail,
        CancellationToken cancellationToken)
    {
        Submission mailSubmission = await LoadSubmissionForMailAsync(submission, cancellationToken);
        string normalizedTemplateCode = string.IsNullOrWhiteSpace(templateCode)
            ? SentToReviewTemplateCode
            : templateCode.Trim().ToUpperInvariant();

        SubmissionStatusMailTemplate template = ResolveStatusTemplate(normalizedTemplateCode);
        string? recipient = ResolveRecipientEmail(mailSubmission, toEmail);

        if (string.IsNullOrWhiteSpace(recipient))
            return;

        string culture = ResolveCulture(mailSubmission);
        string actionUrl = BuildSubmissionDetailsUrl(culture, mailSubmission.Id);
        string recipientName = ResolveRecipientName(mailSubmission, recipient);
        MailBrandingModel branding = await _mailBrandingResolver.ResolveForSubmissionAsync(mailSubmission, cancellationToken);

        RenderedSystemMailTemplate rendered = await _mailTemplateRenderer.RenderAsync(
            new SystemMailTemplateRenderRequest
            {
                LanguageId = mailSubmission.LanguageId,
                Culture = culture,
                SubjectKey = template.SubjectKey,
                TitleKey = template.TitleKey,
                BodyKey = template.BodyKey,
                ActionTextKey = template.ActionTextKey,
                ActionUrl = actionUrl,
                Branding = branding,
                Tokens = new Dictionary<string, string?>
                {
                    ["RecipientName"] = recipientName,
                    ["SubmissionNumber"] = NormalizeSubmissionNumber(mailSubmission),
                    ["SubmissionTitle"] = mailSubmission.Title,
                    ["CongressTitle"] = branding.ContextTitle
                },
                InfoRows = new List<MailInfoRowModel>
                {
                    new() { Label = Label(culture, "Bildiri No", "Submission Code"), Value = NormalizeSubmissionNumber(mailSubmission) },
                    new() { Label = Label(culture, "Bildiri Başlığı", "Submission Title"), Value = mailSubmission.Title },
                    new() { Label = Label(culture, "Kongre", "Congress"), Value = branding.ContextTitle }
                },
                ShowIfNotRequestedMessage = false
            },
            cancellationToken);

        MailOutboxMessage message = new()
        {
            Id = Guid.NewGuid(),
            MailType = ResolveMailType(normalizedTemplateCode),
            RelatedUserId = ResolveRelatedUserId(mailSubmission, recipient),
            RelatedAuthorId = ResolveRelatedAuthorId(mailSubmission, recipient),
            RelatedSubmissionId = mailSubmission.Id,
            CongressId = mailSubmission.CongressId,
            ToEmail = recipient.Trim(),
            ToName = recipientName,
            Subject = rendered.Subject,
            HtmlBody = rendered.HtmlBody
        };

        await ApplySenderSnapshotAsync(message, mailSubmission, cancellationToken);
        await QueueIfNotExistsAsync(message, cancellationToken);
    }

    private async Task ApplySenderSnapshotAsync(
        MailOutboxMessage message,
        Submission submission,
        CancellationToken cancellationToken)
    {
        Guid organizationId = submission.Congress?.OrganizationId
            ?? throw new InvalidOperationException("Bildirinin bağlı olduğu organizasyon bulunamadı.");

        ResolvedOrganizationMailConfiguration sender = await _mailConfigurationResolver.ResolveAsync(
            organizationId,
            cancellationToken);

        message.OrganizationId = organizationId;
        message.CongressId = submission.CongressId;
        message.FromEmail = sender.FromEmail;
        message.FromName = sender.FromName;
        message.ReplyToEmail = sender.ReplyToEmail;
        message.ReplyToName = sender.ReplyToName;
    }

    private async Task QueueIfNotExistsAsync(MailOutboxMessage message, CancellationToken cancellationToken)
    {
        string normalizedRecipient = message.ToEmail.Trim();

        bool alreadyQueued = await _mailOutboxMessageRepository
            .Query()
            .AnyAsync(existing =>
                existing.RelatedSubmissionId == message.RelatedSubmissionId &&
                existing.ToEmail == normalizedRecipient &&
                existing.Subject == message.Subject &&
                existing.DeletedDate == null,
                cancellationToken);

        if (alreadyQueued)
            return;

        message.ToEmail = normalizedRecipient;
        message.CreatedDate = message.CreatedDate == default ? DateTime.UtcNow : message.CreatedDate;
        message.CreatedBy ??= "MailOutboxService";
        await _mailOutboxMessageRepository.AddAsync(message);
    }

    private async Task<Submission> LoadSubmissionForMailAsync(Submission submission, CancellationToken cancellationToken)
    {
        Submission? loadedSubmission = await _submissionRepository
            .Query()
            .Include(item => item.Authors)
                .ThenInclude(author => author.Title)
                    .ThenInclude(title => title!.Translations)
            .Include(item => item.CreatedByUser)
            .Include(item => item.Language)
            .Include(item => item.Congress)
            .FirstOrDefaultAsync(item => item.Id == submission.Id, cancellationToken);

        return loadedSubmission ?? submission;
    }

    private static SubmissionStatusMailTemplate ResolveStatusTemplate(string templateCode)
    {
        return templateCode switch
        {
            AcceptedTemplateCode => new SubmissionStatusMailTemplate(
                SystemMailResourceKeys.SubmissionAcceptedSubject,
                SystemMailResourceKeys.SubmissionAcceptedTitle,
                SystemMailResourceKeys.SubmissionAcceptedBody,
                SystemMailResourceKeys.SubmissionAcceptedButton),
            PaymentPendingTemplateCode => new SubmissionStatusMailTemplate(
                SystemMailResourceKeys.SubmissionPaymentPendingSubject,
                SystemMailResourceKeys.SubmissionPaymentPendingTitle,
                SystemMailResourceKeys.SubmissionPaymentPendingBody,
                SystemMailResourceKeys.SubmissionPaymentPendingButton),
            PaymentApprovedTemplateCode => new SubmissionStatusMailTemplate(
                SystemMailResourceKeys.SubmissionPaymentApprovedSubject,
                SystemMailResourceKeys.SubmissionPaymentApprovedTitle,
                SystemMailResourceKeys.SubmissionPaymentApprovedBody,
                SystemMailResourceKeys.SubmissionPaymentApprovedButton),
            _ => new SubmissionStatusMailTemplate(
                SystemMailResourceKeys.SubmissionSentToReviewSubject,
                SystemMailResourceKeys.SubmissionSentToReviewTitle,
                SystemMailResourceKeys.SubmissionSentToReviewBody,
                SystemMailResourceKeys.SubmissionSentToReviewButton)
        };
    }

    private static MailMessageType ResolveMailType(string templateCode)
    {
        return templateCode switch
        {
            AcceptedTemplateCode => MailMessageType.SubmissionAccepted,
            PaymentPendingTemplateCode => MailMessageType.SubmissionPaymentPending,
            PaymentApprovedTemplateCode => MailMessageType.SubmissionPaymentApproved,
            _ => MailMessageType.SubmissionSentToReview
        };
    }

    private static Guid? ResolveRelatedUserId(Submission submission, string recipientEmail)
    {
        if (submission.CreatedByUserId.HasValue &&
            !string.IsNullOrWhiteSpace(submission.CreatedByUser?.Email) &&
            string.Equals(submission.CreatedByUser.Email.Trim(), recipientEmail.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return submission.CreatedByUserId;
        }

        return null;
    }

    private static Guid? ResolveRelatedAuthorId(Submission submission, string recipientEmail)
    {
        string normalized = recipientEmail.Trim();
        return submission.Authors
            .FirstOrDefault(author =>
                !string.IsNullOrWhiteSpace(author.Email) &&
                string.Equals(author.Email.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    private static string? ResolveRecipientEmail(Submission submission, string? fallbackEmail)
    {
        if (!string.IsNullOrWhiteSpace(fallbackEmail))
            return fallbackEmail.Trim();

        return submission.Authors.FirstOrDefault(author => author.IsCorrespondingAuthor && !string.IsNullOrWhiteSpace(author.Email))?.Email
            ?? submission.Authors.FirstOrDefault(author => !string.IsNullOrWhiteSpace(author.Email))?.Email
            ?? submission.CreatedByUser?.Email;
    }

    private static string ResolveAcceptanceRecipientName(
        Submission submission,
        SubmissionAcceptanceLetter acceptanceLetter,
        string recipientEmail)
    {
        Author? letterAuthor = null;

        if (acceptanceLetter.AuthorId.HasValue)
            letterAuthor = submission.Authors.FirstOrDefault(author => author.Id == acceptanceLetter.AuthorId.Value);

        if (letterAuthor is null && !string.IsNullOrWhiteSpace(acceptanceLetter.AuthorEmailSnapshot))
        {
            string snapshotEmail = acceptanceLetter.AuthorEmailSnapshot.Trim();
            letterAuthor = submission.Authors.FirstOrDefault(author =>
                !string.IsNullOrWhiteSpace(author.Email) &&
                string.Equals(author.Email.Trim(), snapshotEmail, StringComparison.OrdinalIgnoreCase));
        }

        if (letterAuthor is not null)
        {
            string authorDisplayName = ResolveAuthorDisplayName(letterAuthor);
            if (!string.IsNullOrWhiteSpace(authorDisplayName))
                return authorDisplayName;
        }

        return FirstNonEmpty(
            acceptanceLetter.AuthorFullNameSnapshot,
            ResolveRecipientName(submission, recipientEmail),
            recipientEmail);
    }

    private static string ResolveRecipientName(Submission submission, string recipientEmail)
    {
        Author? correspondingAuthor = submission.Authors.FirstOrDefault(author =>
            author.IsCorrespondingAuthor && !string.IsNullOrWhiteSpace(author.Email));

        Author? firstAuthor = correspondingAuthor
            ?? submission.Authors.FirstOrDefault(author => !string.IsNullOrWhiteSpace(author.Email));

        if (firstAuthor is not null)
        {
            string authorDisplayName = ResolveAuthorDisplayName(firstAuthor);
            if (!string.IsNullOrWhiteSpace(authorDisplayName))
                return authorDisplayName;
        }

        string userFullName = $"{submission.CreatedByUser?.Name} {submission.CreatedByUser?.Surname}".Trim();
        return string.IsNullOrWhiteSpace(userFullName) ? recipientEmail : userFullName;
    }

    private static string ResolveAuthorDisplayName(Author author)
    {
        string authorName = $"{NormalizeOptional(author.FirstName)} {NormalizeOptional(author.LastName)}".Trim();
        if (string.IsNullOrWhiteSpace(authorName))
            return string.Empty;

        string authorTitle = ResolveAuthorTitle(author);
        return string.IsNullOrWhiteSpace(authorTitle)
            ? authorName
            : $"{authorTitle} {authorName}";
    }

    private static string ResolveAuthorTitle(Author author)
    {
        if (author.Title is null)
            return string.Empty;

        var preferredTranslation = author.Title.Translations
            .Where(translation => translation.DeletedDate == null)
            .OrderBy(translation => string.IsNullOrWhiteSpace(translation.Description) ? 1 : 0)
            .ThenBy(translation => string.IsNullOrWhiteSpace(translation.Name) ? 1 : 0)
            .FirstOrDefault();

        return FirstNonEmpty(
            preferredTranslation?.Description,
            preferredTranslation?.Name,
            author.Title.Code);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
           ?? string.Empty;

    private string BuildAcceptanceLetterPublicUrl(SubmissionAcceptanceLetter acceptanceLetter)
    {
        string code = Uri.EscapeDataString(
            acceptanceLetter.LetterNumber?.Trim() ?? string.Empty);

        return _publicUrlService.Build(
            $"/public/acceptance-letters/{acceptanceLetter.Id:D}/{code}");
    }

    private string BuildSubmissionDetailsUrl(string culture, Guid submissionId)
    {
        string normalizedCulture = string.IsNullOrWhiteSpace(culture)
            ? "tr-TR"
            : culture.Trim();

        return _publicUrlService.Build(
            $"/{normalizedCulture}/submissions/details/{submissionId:D}");
    }

    private static string ResolveCulture(Submission submission)
        => submission.Language?.Culture ?? "tr-TR";

    private static string NormalizeSubmissionNumber(Submission submission)
    {
        string value = submission.SubmissionNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value) || (value.Count(character => character == '-') >= 3 && value.Length > 14))
            return submission.Id.ToString("N")[..8].ToUpperInvariant();

        return value.ToUpperInvariant();
    }

    private static string Label(string culture, string tr, string en)
        => culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? en : tr;

    private sealed record SubmissionStatusMailTemplate(
        string SubjectKey,
        string TitleKey,
        string BodyKey,
        string ActionTextKey);
}
