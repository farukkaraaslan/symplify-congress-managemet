using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Services;

public sealed class BulkEmailRecipientResolver : IBulkEmailRecipientResolver
{
    private static readonly HashSet<string> AcceptedStatusCodes = new(StringComparer.Ordinal)
    {
        "ACCEPTED"
    };

    private static readonly HashSet<string> PaymentPendingStatusCodes = new(StringComparer.Ordinal)
    {
        "PAYMENTPENDING",
        "PENDING",
        "WAITING",
        "WAITINGPAYMENT"
    };

    private static readonly HashSet<string> PaymentCompletedStatusCodes = new(StringComparer.Ordinal)
    {
        "PAYMENTCOMPLETED",
        "COMPLETED",
        "PAID",
        "PAYMENTPAID",
        "PAYMENTDONE",
        "APPROVED",
        "PAYMENTAPPROVED"
    };

    private readonly ISubmissionRepository _submissionRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public BulkEmailRecipientResolver(
        ISubmissionRepository submissionRepository,
        IOrganizationUserRepository organizationUserRepository)
    {
        _submissionRepository = submissionRepository;
        _organizationUserRepository = organizationUserRepository;
    }

    public async Task<BulkEmailRecipientResolutionResult> ResolveAsync(
        Guid congressId,
        BulkEmailAudienceType audienceType,
        CancellationToken cancellationToken = default)
    {
        // "Tüm Kayıt Olanlara" submission sahiplerini değil,
        // seçilen kongreye kayıt sırasında bağlanmış OrganizationUser kayıtlarını ifade eder.
        if (audienceType == BulkEmailAudienceType.AllRegistered)
        {
            return await ResolveRegisteredUsersAsync(congressId, cancellationToken);
        }

        List<Submission> submissions = await _submissionRepository
            .Query()
            .AsNoTracking()
            .Include(submission => submission.CreatedByUser)
            .Include(submission => submission.Authors)
            .Include(submission => submission.TransactionStatus)
            .Include(submission => submission.PaymentStatus)
            .Where(submission =>
                submission.CongressId == congressId &&
                submission.DeletedDate == null)
            .ToListAsync(cancellationToken);

        List<(string? Email, string? Name)> candidates = new();

        foreach (Submission submission in submissions)
        {
            bool isAccepted = AcceptedStatusCodes.Contains(NormalizeCode(submission.TransactionStatus?.Code));
            bool isPaymentPending = PaymentPendingStatusCodes.Contains(NormalizeCode(submission.PaymentStatus?.Code));
            bool isPaymentCompleted = PaymentCompletedStatusCodes.Contains(NormalizeCode(submission.PaymentStatus?.Code));

            switch (audienceType)
            {
                case BulkEmailAudienceType.AcceptedCorrespondingAuthors when isAccepted:
                    candidates.Add(ResolvePrimaryRecipient(submission));
                    break;

                case BulkEmailAudienceType.AcceptedAllAuthors when isAccepted:
                    candidates.AddRange(ResolveAllAuthors(submission));
                    break;

                case BulkEmailAudienceType.PaymentPending when isAccepted && isPaymentPending:
                    candidates.Add(ResolvePrimaryRecipient(submission));
                    break;

                case BulkEmailAudienceType.PaymentCompleted when isAccepted && isPaymentCompleted:
                    candidates.Add(ResolvePrimaryRecipient(submission));
                    break;
            }
        }

        return BuildResolutionResult(candidates);
    }

    public async Task<BulkEmailRecipientResolutionResult> ResolveAdjustedAsync(
        Guid congressId,
        BulkEmailAudienceType audienceType,
        IReadOnlyCollection<string>? excludedRecipientEmails,
        IReadOnlyCollection<BulkEmailRecipientDto>? additionalRecipients,
        CancellationToken cancellationToken = default)
    {
        BulkEmailRecipientResolutionResult baseResult = await ResolveAsync(
            congressId,
            audienceType,
            cancellationToken);

        Dictionary<string, BulkEmailRecipientDto> baseRecipients = baseResult.Recipients
            .ToDictionary(recipient => recipient.Email, StringComparer.OrdinalIgnoreCase);

        HashSet<string> excludedEmails = new(StringComparer.OrdinalIgnoreCase);
        foreach (string email in excludedRecipientEmails ?? Array.Empty<string>())
        {
            if (TryNormalizeEmail(email, out string canonicalEmail))
                excludedEmails.Add(canonicalEmail);
        }

        Dictionary<string, BulkEmailRecipientDto> finalRecipients = baseRecipients
            .Where(pair => !excludedEmails.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        int invalidEmailCount = baseResult.InvalidEmailCount;

        foreach (BulkEmailRecipientDto recipient in additionalRecipients ?? Array.Empty<BulkEmailRecipientDto>())
        {
            if (!TryNormalizeEmail(recipient.Email, out string canonicalEmail))
            {
                invalidEmailCount++;
                continue;
            }

            // Manually adding an address that belongs to the selected audience restores it if it was excluded.
            if (baseRecipients.TryGetValue(canonicalEmail, out BulkEmailRecipientDto? baseRecipient))
            {
                finalRecipients[canonicalEmail] = baseRecipient;
                continue;
            }

            finalRecipients[canonicalEmail] = new BulkEmailRecipientDto
            {
                Email = canonicalEmail,
                Name = NormalizeName(recipient.Name, canonicalEmail),
                IsManual = true
            };
        }

        return new BulkEmailRecipientResolutionResult
        {
            Recipients = OrderRecipients(finalRecipients.Values),
            InvalidEmailCount = invalidEmailCount
        };
    }

    private async Task<BulkEmailRecipientResolutionResult> ResolveRegisteredUsersAsync(
        Guid congressId,
        CancellationToken cancellationToken)
    {
        var registeredUsers = await _organizationUserRepository
            .Query()
            .AsNoTracking()
            .Where(item =>
                item.DefaultCongressId == congressId &&
                item.IsActive &&
                item.DeletedDate == null &&
                item.User.DeletedDate == null &&
                !item.User.IsBlacklisted)
            .Select(item => new
            {
                item.User.Email,
                item.User.Name,
                item.User.Surname
            })
            .ToListAsync(cancellationToken);

        IEnumerable<(string? Email, string? Name)> candidates = registeredUsers.Select(user =>
        {
            string fullName = $"{user.Name} {user.Surname}".Trim();
            return (user.Email, (string?)fullName);
        });

        return BuildResolutionResult(candidates);
    }

    private static BulkEmailRecipientResolutionResult BuildResolutionResult(
        IEnumerable<(string? Email, string? Name)> candidates)
    {
        Dictionary<string, BulkEmailRecipientDto> uniqueRecipients = new(StringComparer.OrdinalIgnoreCase);
        int invalidEmailCount = 0;

        foreach ((string? email, string? name) in candidates)
        {
            if (!TryNormalizeEmail(email, out string canonicalEmail))
            {
                invalidEmailCount++;
                continue;
            }

            if (uniqueRecipients.ContainsKey(canonicalEmail))
                continue;

            uniqueRecipients[canonicalEmail] = new BulkEmailRecipientDto
            {
                Email = canonicalEmail,
                Name = NormalizeName(name, canonicalEmail),
                IsManual = false
            };
        }

        return new BulkEmailRecipientResolutionResult
        {
            Recipients = OrderRecipients(uniqueRecipients.Values),
            InvalidEmailCount = invalidEmailCount
        };
    }

    private static IReadOnlyList<BulkEmailRecipientDto> OrderRecipients(IEnumerable<BulkEmailRecipientDto> recipients)
    {
        return recipients
            .OrderBy(recipient => recipient.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(recipient => recipient.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool TryNormalizeEmail(string? value, out string canonicalEmail)
    {
        canonicalEmail = string.Empty;
        string normalizedEmail = value?.Trim() ?? string.Empty;

        if (!MailAddress.TryCreate(normalizedEmail, out MailAddress? parsedAddress))
            return false;

        canonicalEmail = parsedAddress.Address.Trim().ToLowerInvariant();
        return canonicalEmail.Length > 0 && canonicalEmail.Length <= 320;
    }

    private static string NormalizeName(string? value, string fallback)
    {
        string normalized = (value ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            return fallback;

        return normalized.Length <= 250 ? normalized : normalized[..250];
    }

    private static (string? Email, string? Name) ResolvePrimaryRecipient(Submission submission)
    {
        Author? author = submission.Authors
            .Where(item => item.DeletedDate == null && !string.IsNullOrWhiteSpace(item.Email))
            .OrderByDescending(item => item.IsCorrespondingAuthor)
            .ThenBy(item => item.CreatedDate)
            .FirstOrDefault();

        if (author is not null)
            return (author.Email, BuildAuthorName(author));

        if (submission.CreatedByUser is not null &&
            submission.CreatedByUser.DeletedDate == null &&
            !submission.CreatedByUser.IsBlacklisted)
        {
            string userName = $"{submission.CreatedByUser.Name} {submission.CreatedByUser.Surname}".Trim();
            return (submission.CreatedByUser.Email, userName);
        }

        return (null, null);
    }

    private static IEnumerable<(string? Email, string? Name)> ResolveAllAuthors(Submission submission)
    {
        foreach (Author author in submission.Authors
                     .Where(item => item.DeletedDate == null && !string.IsNullOrWhiteSpace(item.Email))
                     .OrderByDescending(item => item.IsCorrespondingAuthor)
                     .ThenBy(item => item.CreatedDate))
        {
            yield return (author.Email, BuildAuthorName(author));
        }
    }

    private static string BuildAuthorName(Author author)
        => $"{author.FirstName} {author.LastName}".Trim();

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim().ToUpperInvariant()
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal);

        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }
}
