using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Core.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Mailing;

namespace Symplify.BackOffice.Infrastructure.Email;

public sealed class SmtpBackOfficeEmailSender : IBackOfficeEmailSender
{
    private readonly IObjectStorageService _objectStorageService;
    private readonly IOrganizationMailConfigurationResolver _organizationMailConfigurationResolver;
    private readonly ILogger<SmtpBackOfficeEmailSender> _logger;
    private readonly BackOfficeMailOptions _mailOptions;

    public SmtpBackOfficeEmailSender(
        IObjectStorageService objectStorageService,
        IOrganizationMailConfigurationResolver organizationMailConfigurationResolver,
        IOptions<BackOfficeMailOptions> mailOptions,
        ILogger<SmtpBackOfficeEmailSender> logger)
    {
        _objectStorageService = objectStorageService;
        _organizationMailConfigurationResolver = organizationMailConfigurationResolver;
        _mailOptions = mailOptions.Value;
        _logger = logger;
    }

    public async Task<BackOfficeEmailSendResult> SendAsync(BackOfficeEmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.OrganizationId == Guid.Empty)
            throw new InvalidOperationException("OrganizationId is required for every outgoing email.");

        ResolvedOrganizationMailConfiguration resolved = await _organizationMailConfigurationResolver.ResolveAsync(
            message.OrganizationId,
            cancellationToken);

        EffectiveSmtpConfiguration configuration = new()
        {
            Host = resolved.Host,
            Port = resolved.Port,
            EnableSsl = resolved.EnableSsl,
            Username = resolved.Username,
            Password = resolved.Password,
            FromEmail = FirstNotEmpty(message.FromEmail, resolved.FromEmail),
            FromName = FirstNotEmpty(message.FromName, resolved.FromName),
            ReplyToEmail = FirstNotEmptyOrNull(message.ReplyToEmail, resolved.ReplyToEmail),
            ReplyToName = FirstNotEmptyOrNull(message.ReplyToName, resolved.ReplyToName)
        };

        ValidateConfiguration(configuration);

        using MailMessage mailMessage = new();
        mailMessage.From = new MailAddress(configuration.FromEmail, configuration.FromName);
        mailMessage.To.Add(new MailAddress(message.ToEmail, message.ToName));
        mailMessage.Subject = message.Subject;

        if (!string.IsNullOrWhiteSpace(configuration.ReplyToEmail))
        {
            mailMessage.ReplyToList.Add(new MailAddress(
                configuration.ReplyToEmail,
                configuration.ReplyToName));
        }

        await AddHtmlBodyAsync(mailMessage, message.HtmlBody, resolved, cancellationToken);
        await AddAttachmentAsync(mailMessage, message, cancellationToken);

        bool sesTrackingEnabled = TryAddAmazonSesTrackingHeaders(mailMessage, message, configuration.Host);

        using SmtpClient client = new(configuration.Host, configuration.Port)
        {
            EnableSsl = configuration.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(configuration.Username, configuration.Password)
        };

        await client.SendMailAsync(mailMessage, cancellationToken);

        return new BackOfficeEmailSendResult
        {
            Provider = IsAmazonSesHost(configuration.Host) ? "AmazonSES" : "SMTP",
            DeliveryTrackingEnabled = sesTrackingEnabled
        };
    }

    private bool TryAddAmazonSesTrackingHeaders(
        MailMessage mailMessage,
        BackOfficeEmailMessage message,
        string smtpHost)
    {
        SesMailTrackingOptions tracking = _mailOptions.SesTracking;
        if (!tracking.Enabled ||
            !IsAmazonSesHost(smtpHost) ||
            message.TrackingId is null ||
            message.TrackingId == Guid.Empty ||
            string.IsNullOrWhiteSpace(tracking.ConfigurationSetName))
        {
            return false;
        }

        string configurationSet = tracking.ConfigurationSetName.Trim();
        if (configurationSet.Contains('\r') || configurationSet.Contains('\n'))
            throw new InvalidOperationException("Mail:SesTracking:ConfigurationSetName contains invalid characters.");

        mailMessage.Headers.Add("X-SES-CONFIGURATION-SET", configurationSet);
        mailMessage.Headers.Add(
            "X-SES-MESSAGE-TAGS",
            $"symplifyOutboxId={message.TrackingId.Value:N},mailType={NormalizeSesTagValue(message.MailType.ToString())}");

        return true;
    }

    private static bool IsAmazonSesHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        string normalized = host.Trim().TrimEnd('.');
        return normalized.StartsWith("email-smtp.", StringComparison.OrdinalIgnoreCase) &&
               normalized.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSesTagValue(string value)
    {
        string normalized = new(value.Where(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' or '.').ToArray());

        return string.IsNullOrWhiteSpace(normalized) ? "Unknown" : normalized;
    }

    private async Task AddHtmlBodyAsync(
        MailMessage mailMessage,
        string htmlBody,
        ResolvedOrganizationMailConfiguration configuration,
        CancellationToken cancellationToken)
    {
        AlternateView htmlView = AlternateView.CreateAlternateViewFromString(
            htmlBody ?? string.Empty,
            Encoding.UTF8,
            MediaTypeNames.Text.Html);

        bool htmlReferencesOrganizationLogo = (htmlBody ?? string.Empty).Contains(
            $"cid:{MailBrandingModel.OrganizationLogoContentId}",
            StringComparison.OrdinalIgnoreCase);

        if (htmlReferencesOrganizationLogo &&
            !string.IsNullOrWhiteSpace(configuration.MailLogoBucketName) &&
            !string.IsNullOrWhiteSpace(configuration.MailLogoObjectName))
        {
            try
            {
                Stream logoStream = await _objectStorageService.OpenReadAsync(
                    configuration.MailLogoBucketName.Trim(),
                    configuration.MailLogoObjectName.Trim(),
                    cancellationToken);

                string contentType = NormalizeLogoContentType(configuration.MailLogoContentType);
                LinkedResource logoResource = new(logoStream, contentType)
                {
                    ContentId = MailBrandingModel.OrganizationLogoContentId,
                    TransferEncoding = TransferEncoding.Base64
                };

                logoResource.ContentType.Name = string.IsNullOrWhiteSpace(configuration.MailLogoFileName)
                    ? Path.GetFileName(configuration.MailLogoObjectName)
                    : configuration.MailLogoFileName.Trim();

                htmlView.LinkedResources.Add(logoResource);
            }
            catch (Exception exception)
            {
                // A missing logo must not block transactional mail delivery. The image alt text
                // remains visible in clients that cannot load the CID resource.
                _logger.LogWarning(
                    exception,
                    "Private organization mail logo could not be attached. OrganizationId: {OrganizationId}, Object: {ObjectName}",
                    configuration.OrganizationId,
                    configuration.MailLogoObjectName);
            }
        }

        mailMessage.AlternateViews.Add(htmlView);
        mailMessage.IsBodyHtml = true;
    }

    private async Task AddAttachmentAsync(
        MailMessage mailMessage,
        BackOfficeEmailMessage message,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(message.AttachmentBucketName) &&
            !string.IsNullOrWhiteSpace(message.AttachmentObjectName))
        {
            Stream attachmentStream = await _objectStorageService.OpenReadAsync(
                message.AttachmentBucketName.Trim(),
                message.AttachmentObjectName.Trim(),
                cancellationToken);

            string fileName = string.IsNullOrWhiteSpace(message.AttachmentFileName)
                ? Path.GetFileName(message.AttachmentObjectName)
                : message.AttachmentFileName.Trim();

            string contentType = string.IsNullOrWhiteSpace(message.AttachmentContentType)
                ? "application/octet-stream"
                : message.AttachmentContentType.Trim();

            mailMessage.Attachments.Add(new Attachment(attachmentStream, fileName, contentType));
            return;
        }

        if (!string.IsNullOrWhiteSpace(message.AttachmentPath) && File.Exists(message.AttachmentPath))
            mailMessage.Attachments.Add(new Attachment(message.AttachmentPath));
    }

    private static string NormalizeLogoContentType(string? contentType)
    {
        return contentType?.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => "image/jpeg",
            "image/jpg" => "image/jpeg",
            _ => "image/png"
        };
    }

    private static void ValidateConfiguration(EffectiveSmtpConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Host))
            throw new InvalidOperationException("SMTP host is required.");

        if (configuration.Port is <= 0 or > 65535)
            throw new InvalidOperationException("SMTP port is invalid.");

        if (string.IsNullOrWhiteSpace(configuration.Username))
            throw new InvalidOperationException("SMTP username is required.");

        if (string.IsNullOrWhiteSpace(configuration.Password))
            throw new InvalidOperationException("SMTP password is required.");

        if (string.IsNullOrWhiteSpace(configuration.FromEmail))
            throw new InvalidOperationException("Mail sender address is required.");
    }

    private static string FirstNotEmpty(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred)
            ? preferred.Trim()
            : fallback?.Trim() ?? string.Empty;

    private static string? FirstNotEmptyOrNull(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred)
            ? preferred.Trim()
            : string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();

    private sealed class EffectiveSmtpConfiguration
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; }
        public bool EnableSsl { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string FromEmail { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
        public string? ReplyToEmail { get; init; }
        public string? ReplyToName { get; init; }
    }
}
