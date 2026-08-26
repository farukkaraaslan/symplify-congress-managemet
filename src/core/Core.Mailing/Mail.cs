using MimeKit;

namespace Core.Mailing;

public class Mail
{
    public required string Subject { get; init; }
    public required string TextBody { get; init; }
    public required string HtmlBody { get; init; }
    public AttachmentCollection? Attachments { get; init; }
    public required List<MailboxAddress> ToList { get; init; }
    public List<MailboxAddress>? CcList { get; init; }
    public List<MailboxAddress>? BccList { get; init; }
    public string? UnscribeLink { get; init; }

    public Mail() { }

    public Mail(
        string subject,
        string textBody,
        string htmlBody,
        AttachmentCollection? attachments,
        List<MailboxAddress> toList,
        List<MailboxAddress>? ccList = null,
        List<MailboxAddress>? bccList = null,
        string? unscribeLink = null
    )
    {
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
        Attachments = attachments;
        ToList = toList;
        CcList = ccList;
        BccList = bccList;
        UnscribeLink = unscribeLink;
    }
}
