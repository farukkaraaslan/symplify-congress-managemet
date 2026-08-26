using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Core.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Services.ParticipationCertificates;

public sealed partial class ParticipationCertificateService : IParticipationCertificateService
{
    private static readonly Regex InvalidFileNameChars = new("[^a-z0-9._-]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> AcceptedStatusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACCEPTED",
        "KABULEDILDI"
    };

    private static readonly HashSet<string> PaidPaymentStatusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PAYMENTCOMPLETED",
        "PAYMENT_COMPLETED",
        "COMPLETED",
        "PAID",
        "PAYMENTPAID",
        "PAYMENTDONE",
        "APPROVED",
        "PAYMENTAPPROVED"
    };

    private readonly BackOfficeDbContext _context;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly IParticipationCertificatePdfRenderer _pdfRenderer;
    private readonly ISystemMailTemplateRenderer _mailTemplateRenderer;
    private readonly IMailBrandingResolver _mailBrandingResolver;
    private readonly IOrganizationMailConfigurationResolver _mailConfigurationResolver;
    private readonly IPublicUrlService _publicUrlService;
    private readonly ILogger<ParticipationCertificateService> _logger;

    public ParticipationCertificateService(
        BackOfficeDbContext context,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions,
        IParticipationCertificatePdfRenderer pdfRenderer,
        ISystemMailTemplateRenderer mailTemplateRenderer,
        IMailBrandingResolver mailBrandingResolver,
        IOrganizationMailConfigurationResolver mailConfigurationResolver,
        IPublicUrlService publicUrlService,
        ILogger<ParticipationCertificateService> logger)
    {
        _context = context;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
        _pdfRenderer = pdfRenderer;
        _mailTemplateRenderer = mailTemplateRenderer;
        _mailBrandingResolver = mailBrandingResolver;
        _mailConfigurationResolver = mailConfigurationResolver;
        _publicUrlService = publicUrlService;
        _logger = logger;
    }


    public async Task<IReadOnlyList<ParticipationCertificateCongressOptionDto>> GetCongressOptionsAsync(
        string? culture,
        Guid? includeCongressId = null,
        CancellationToken cancellationToken = default)
    {
        string normalizedCulture = string.IsNullOrWhiteSpace(culture) ? "tr-TR" : culture.Trim();

        Guid? languageId = await _context.Languages
            .AsNoTracking()
            .Where(item => item.DeletedDate == null && item.IsActive && item.Culture == normalizedCulture)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Guid? defaultLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(item => item.DeletedDate == null && item.IsActive && item.IsDefault)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        List<Congress> congresses = await _context.Congresses
            .AsNoTracking()
            .Include(item => item.Translations)
            .Where(item =>
                item.DeletedDate == null &&
                (item.Status == CongressStatus.Published ||
                 (includeCongressId.HasValue && item.Id == includeCongressId.Value)))
            .OrderByDescending(item => item.StartDate ?? item.CreatedDate)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return congresses
            .Select(congress => new ParticipationCertificateCongressOptionDto
            {
                Id = congress.Id,
                Text = BuildCongressOptionText(congress, languageId, defaultLanguageId)
            })
            .ToList();
    }

    public async Task<ParticipationCertificateDashboardDto> GetDashboardAsync(
        Guid congressId,
        string? culture,
        ParticipationCertificateDashboardFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        CongressInfo congress = await ResolveCongressAsync(congressId, culture, cancellationToken);
        ParticipationCertificateDashboardFilter resolvedFilter = filter ?? GetDefaultEligibilityFilter();
        IReadOnlyList<ParticipationCertificateTemplate> templates = await GetActiveTemplatesAsync(congressId, cancellationToken);
        string certificateCulture = ResolveDashboardCertificateCulture(resolvedFilter.CertificateCulture, templates);
        ParticipationCertificateTemplate? template = templates.FirstOrDefault(item =>
            string.Equals(item.Culture, certificateCulture, StringComparison.OrdinalIgnoreCase));
        string defaultCertificateCulture = templates.FirstOrDefault(item => item.IsDefault)?.Culture
            ?? templates.FirstOrDefault()?.Culture
            ?? ParticipationCertificateCultures.Turkish;

        IReadOnlyList<ParticipationCertificateFilterOptionDto> submissionStatusOptions = await BuildSubmissionStatusOptionsAsync(congressId, culture, cancellationToken);
        IReadOnlyList<ParticipationCertificateFilterOptionDto> paymentStatusOptions = await BuildPaymentStatusOptionsAsync(congressId, culture, cancellationToken);
        IReadOnlyList<ParticipationCertificateCandidateDto> allCandidates = await BuildCandidatesAsync(
            congressId,
            culture,
            certificateCulture,
            resolvedFilter,
            cancellationToken);

        IReadOnlyList<ParticipationCertificateCandidateDto> searchedCandidates = ApplyCandidateSearch(
            allCandidates,
            resolvedFilter.CandidateSearch);
        int pageSize = Math.Clamp(resolvedFilter.CandidatePageSize <= 0 ? 100 : resolvedFilter.CandidatePageSize, 25, 250);
        int totalPages = Math.Max(1, (int)Math.Ceiling(searchedCandidates.Count / (double)pageSize));
        int page = Math.Clamp(resolvedFilter.CandidatePage <= 0 ? 1 : resolvedFilter.CandidatePage, 1, totalPages);
        IReadOnlyList<ParticipationCertificateCandidateDto> pagedCandidates = searchedCandidates
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        ParticipationCertificateGenerationJobDto? generationJob = await GetLatestGenerationJobAsync(
            congressId,
            certificateCulture,
            cancellationToken);

        IQueryable<ParticipationCertificate> activeCertificateQuery = _context.ParticipationCertificates
            .AsNoTracking()
            .Where(item =>
                item.CongressId == congressId &&
                item.Culture == certificateCulture &&
                item.DeletedDate == null);

        int generatedCount = await activeCertificateQuery.CountAsync(cancellationToken);
        int emailQueuedCount = await activeCertificateQuery.CountAsync(item =>
            item.EmailSentAt == null &&
            (item.EmailStatus == "QueueRequested" ||
             item.EmailStatus == "QueuePreparing" ||
             item.EmailStatus == "Queued"),
            cancellationToken);
        int emailSentCount = await activeCertificateQuery.CountAsync(item => item.EmailSentAt != null, cancellationToken);
        int mailSelectableCount = await activeCertificateQuery.CountAsync(item =>
            item.EmailSentAt == null &&
            item.AuthorEmailSnapshot != null &&
            item.AuthorEmailSnapshot != string.Empty &&
            (item.EmailStatus == null ||
             (item.EmailStatus != "QueueRequested" &&
              item.EmailStatus != "QueuePreparing" &&
              item.EmailStatus != "Queued")),
            cancellationToken);
        int revokedCount = await _context.ParticipationCertificates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(item =>
                item.CongressId == congressId &&
                item.Culture == certificateCulture &&
                item.RevokedAt != null,
                cancellationToken);

        return new ParticipationCertificateDashboardDto
        {
            CongressId = congressId,
            CongressTitle = congress.Title,
            Filter = resolvedFilter,
            CertificateCulture = certificateCulture,
            DefaultCertificateCulture = defaultCertificateCulture,
            Template = template is null ? null : MapTemplate(template),
            Templates = templates.Select(MapTemplate).ToList(),
            SubmissionStatusOptions = submissionStatusOptions,
            PaymentStatusOptions = paymentStatusOptions,
            Candidates = pagedCandidates,
            CandidateCount = searchedCandidates.Count,
            EligibleCandidateCount = searchedCandidates.Count(candidate => candidate.IsEligible),
            GeneratedCount = generatedCount,
            EmailQueuedCount = emailQueuedCount,
            EmailSentCount = emailSentCount,
            RevokedCount = revokedCount,
            MissingEmailCount = searchedCandidates.Count(candidate => string.IsNullOrWhiteSpace(candidate.AuthorEmail)),
            MailSelectableCount = mailSelectableCount,
            CandidatePage = page,
            CandidatePageSize = pageSize,
            CandidateTotalPages = totalPages,
            CandidateSearch = resolvedFilter.CandidateSearch,
            GenerationJob = generationJob
        };
    }

    public async Task<ParticipationCertificateStoredFileDto?> GetGeneratedFileAsync(
        Guid certificateId,
        CancellationToken cancellationToken = default)
    {
        if (certificateId == Guid.Empty)
            return null;

        return await _context.ParticipationCertificates
            .AsNoTracking()
            .Where(item => item.Id == certificateId && item.DeletedDate == null)
            .Select(item => new ParticipationCertificateStoredFileDto
            {
                Id = item.Id,
                CongressId = item.CongressId,
                SubmissionNumber = item.SubmissionNumber,
                AuthorFullName = item.AuthorFullNameSnapshot,
                Culture = item.Culture,
                FileName = item.FileName,
                ContentType = item.ContentType,
                BucketName = item.BucketName,
                ObjectName = item.ObjectName
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ParticipationCertificateStoredFileDto>> GetGeneratedFilesAsync(
        Guid congressId,
        IReadOnlyCollection<Guid>? certificateIds = null,
        CancellationToken cancellationToken = default)
    {
        if (congressId == Guid.Empty)
            return Array.Empty<ParticipationCertificateStoredFileDto>();

        IQueryable<ParticipationCertificate> query = _context.ParticipationCertificates
            .AsNoTracking()
            .Where(item => item.CongressId == congressId && item.DeletedDate == null);

        if (certificateIds is { Count: > 0 })
        {
            List<Guid> ids = certificateIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return Array.Empty<ParticipationCertificateStoredFileDto>();

            query = query.Where(item => ids.Contains(item.Id));
        }

        return await query
            .OrderBy(item => item.SubmissionNumber)
            .ThenBy(item => item.AuthorFullNameSnapshot)
            .Select(item => new ParticipationCertificateStoredFileDto
            {
                Id = item.Id,
                CongressId = item.CongressId,
                SubmissionNumber = item.SubmissionNumber,
                AuthorFullName = item.AuthorFullNameSnapshot,
                Culture = item.Culture,
                FileName = item.FileName,
                ContentType = item.ContentType,
                BucketName = item.BucketName,
                ObjectName = item.ObjectName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ParticipationCertificateTemplateDto> UploadTemplateAsync(
        ParticipationCertificateTemplateUploadInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.CongressId == Guid.Empty)
            throw new InvalidOperationException("Kongre bilgisi geçersiz.");

        if (input.Content == Stream.Null || input.Length <= 0)
            throw new InvalidOperationException("Katılım belgesi template PDF dosyası seçilmelidir.");

        string extension = Path.GetExtension(input.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Katılım belgesi template dosyası PDF formatında olmalıdır.");

        string templateCulture = ParticipationCertificateCultures.Normalize(input.Culture);
        if (!ParticipationCertificateCultures.IsSupported(templateCulture))
            throw new InvalidOperationException("Katılım belgesi dili yalnızca Türkçe veya İngilizce olabilir.");

        string normalizedBodyText = NormalizeCertificateBodyText(input.BodyText);
        if (string.IsNullOrWhiteSpace(normalizedBodyText))
            throw new InvalidOperationException("Sertifika metni zorunludur. Metin kaydedilmeden template kaydedilemez.");

        if (normalizedBodyText.Length > 4000)
            throw new InvalidOperationException("Sertifika metni en fazla 4000 karakter olabilir.");

        string mailSubject = NormalizeMailSubject(input.MailSubject);
        string mailTitle = NormalizeMailTitle(input.MailTitle);
        string mailBodyHtml = NormalizeMailBodyHtml(input.MailBodyHtml);
        ValidateMailTemplate(mailSubject, mailBodyHtml);

        await using MemoryStream validatedTemplateStream = new();
        await input.Content.CopyToAsync(validatedTemplateStream, cancellationToken);
        byte[] templateBytes = validatedTemplateStream.ToArray();
        _pdfRenderer.ValidateTemplate(templateBytes);
        validatedTemplateStream.Position = 0;

        await EnsureCongressExistsAsync(input.CongressId, cancellationToken);

        string bucketName = GetSubmissionsBucketName();
        Guid templateId = Guid.NewGuid();
        string languageCode = ParticipationCertificateCultures.GetShortCode(templateCulture);
        string fileName = $"participation-certificate-template-{languageCode}-{templateId:N}.pdf";
        string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
            "participation-certificates",
            "templates",
            input.CongressId.ToString("N"),
            languageCode,
            fileName);

        ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
            new ObjectStorageUploadRequest
            {
                BucketName = bucketName,
                ObjectName = objectName,
                OriginalFileName = fileName,
                ContentType = "application/pdf",
                Size = templateBytes.LongLength,
                Content = validatedTemplateStream,
                Metadata = new Dictionary<string, string>
                {
                    ["module"] = "participation-certificates",
                    ["congress-id"] = input.CongressId.ToString("N"),
                    ["template-id"] = templateId.ToString("N"),
                    ["culture"] = templateCulture
                }
            },
            cancellationToken);

        DateTime now = DateTime.UtcNow;
        List<ParticipationCertificateTemplate> congressTemplates = await _context.ParticipationCertificateTemplates
            .Where(item => item.CongressId == input.CongressId && item.DeletedDate == null)
            .ToListAsync(cancellationToken);

        List<ParticipationCertificateTemplate> previousTemplates = congressTemplates
            .Where(item =>
                item.IsActive &&
                string.Equals(item.Culture, templateCulture, StringComparison.OrdinalIgnoreCase))
            .ToList();

        bool replacedTemplateWasDefault = previousTemplates.Any(item => item.IsDefault);
        bool hasAnotherDefault = congressTemplates.Any(item =>
            item.IsActive &&
            item.IsDefault &&
            !previousTemplates.Contains(item));

        bool makeDefault = input.IsDefault || replacedTemplateWasDefault || !hasAnotherDefault;
        foreach (ParticipationCertificateTemplate item in previousTemplates)
        {
            item.IsActive = false;
            item.IsDefault = false;
            item.UpdatedDate = now;
            item.UpdatedBy = "ParticipationCertificateTemplateReplaced";
        }

        if (makeDefault)
        {
            foreach (ParticipationCertificateTemplate item in congressTemplates.Where(item => item.IsDefault))
            {
                item.IsDefault = false;
                item.UpdatedDate = now;
                item.UpdatedBy = "ParticipationCertificateDefaultChanged";
            }
        }

        bool isEnglishTemplate = string.Equals(
            templateCulture,
            ParticipationCertificateCultures.English,
            StringComparison.OrdinalIgnoreCase);
        string fontColorHex = NormalizeHexColor(
            input.NameFontColorHex,
            isEnglishTemplate ? "#0F3791" : "#FFFFFF");

        ParticipationCertificateTemplate template = new()
        {
            Id = templateId,
            CongressId = input.CongressId,
            Name = string.Equals(templateCulture, ParticipationCertificateCultures.English, StringComparison.OrdinalIgnoreCase)
                ? "Certificate of Participation"
                : "Katılım Belgesi",
            Culture = templateCulture,
            IsDefault = makeDefault,
            BodyText = normalizedBodyText,
            MailSubject = mailSubject,
            MailTitle = mailTitle,
            MailBodyHtml = mailBodyHtml,
            IsActive = true,
            StorageProvider = _storageOptions.Provider,
            BucketName = uploadResult.BucketName,
            ObjectName = uploadResult.ObjectName,
            FileName = uploadResult.OriginalFileName,
            ContentType = uploadResult.ContentType,
            FileSize = uploadResult.Size,
            ETag = uploadResult.ETag,
            // Koordinat bilgisi artık UI'dan alınmıyor.
            // Renderer konumları template PDF içindeki placeholder değişkenlerinden otomatik bulur.
            // Eski kolonlar geriye dönük uyumluluk için default değerlerle tutuluyor.
            NameBoxX = 120f,
            NameBoxY = 275f,
            NameBoxWidth = 600f,
            NameBoxHeight = 70f,
            NameFontSize = 30f,
            NameFontColorHex = fontColorHex,
            CoverPlaceholderBackground = false,
            PlaceholderBackgroundColorHex = isEnglishTemplate ? "#F4F9FF" : "#06142E",
            RenderCommitteeSignature = input.RenderCommitteeSignature,
            CommitteeSignatureBoxX = 515f,
            CommitteeSignatureBoxY = 112f,
            CommitteeSignatureBoxWidth = 135f,
            CommitteeSignatureBoxHeight = 55f,
            UploadedAt = now,
            CreatedDate = now,
            CreatedBy = "ParticipationCertificateTemplateUpload"
        };

        _context.ParticipationCertificateTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return MapTemplate(template);
    }

    public async Task SetDefaultTemplateAsync(
        Guid congressId,
        string? culture,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (congressId == Guid.Empty)
            throw new InvalidOperationException("Kongre bilgisi geçersiz.");

        string resolvedCulture = ParticipationCertificateCultures.Normalize(culture);
        if (!ParticipationCertificateCultures.IsSupported(resolvedCulture))
            throw new InvalidOperationException("Katılım sertifikası dili yalnızca Türkçe veya İngilizce olabilir.");

        List<ParticipationCertificateTemplate> templates = await _context.ParticipationCertificateTemplates
            .Where(item =>
                item.CongressId == congressId &&
                item.IsActive &&
                item.DeletedDate == null)
            .ToListAsync(cancellationToken);

        ParticipationCertificateTemplate selected = templates.FirstOrDefault(item =>
            string.Equals(item.Culture, resolvedCulture, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"{ParticipationCertificateCultures.GetDisplayName(resolvedCulture)} template PDF dosyası bulunamadı.");

        string actor = performedByUserId?.ToString("D") ?? "ParticipationCertificateDefaultChanged";
        DateTime now = DateTime.UtcNow;

        foreach (ParticipationCertificateTemplate template in templates)
        {
            template.IsDefault = template.Id == selected.Id;
            template.UpdatedDate = now;
            template.UpdatedBy = actor;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveTemplateSettingsAsync(
        Guid congressId,
        string? culture,
        string? bodyText,
        string? nameFontColorHex,
        string? mailSubject,
        string? mailTitle,
        string? mailBodyHtml,
        bool isDefault,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (congressId == Guid.Empty)
            throw new InvalidOperationException("Kongre bilgisi geçersiz.");

        string resolvedCulture = ParticipationCertificateCultures.Normalize(culture);
        if (!ParticipationCertificateCultures.IsSupported(resolvedCulture))
            throw new InvalidOperationException("Katılım sertifikası dili yalnızca Türkçe veya İngilizce olabilir.");

        string normalizedBodyText = NormalizeCertificateBodyText(bodyText);
        if (string.IsNullOrWhiteSpace(normalizedBodyText))
            throw new InvalidOperationException("Sertifika metni zorunludur. Metin kaydedilmeden belge oluşturulamaz.");

        if (normalizedBodyText.Length > 4000)
            throw new InvalidOperationException("Sertifika metni en fazla 4000 karakter olabilir.");

        string normalizedMailSubject = NormalizeMailSubject(mailSubject);
        string normalizedMailTitle = NormalizeMailTitle(mailTitle);
        string normalizedMailBodyHtml = NormalizeMailBodyHtml(mailBodyHtml);
        ValidateMailTemplate(normalizedMailSubject, normalizedMailBodyHtml);

        List<ParticipationCertificateTemplate> templates = await _context.ParticipationCertificateTemplates
            .Where(item =>
                item.CongressId == congressId &&
                item.IsActive &&
                item.DeletedDate == null)
            .ToListAsync(cancellationToken);

        ParticipationCertificateTemplate template = templates.FirstOrDefault(item =>
            string.Equals(item.Culture, resolvedCulture, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"{ParticipationCertificateCultures.GetDisplayName(resolvedCulture)} template PDF dosyası bulunamadı. Önce template yükleyin.");

        string fallbackColor = string.Equals(
            resolvedCulture,
            ParticipationCertificateCultures.English,
            StringComparison.OrdinalIgnoreCase)
                ? "#0F3791"
                : "#0F3791";

        string actor = performedByUserId?.ToString("D") ?? "ParticipationCertificateTemplateSettingsUpdated";
        DateTime now = DateTime.UtcNow;

        template.BodyText = normalizedBodyText;
        template.MailSubject = normalizedMailSubject;
        template.MailTitle = normalizedMailTitle;
        template.MailBodyHtml = normalizedMailBodyHtml;
        template.NameFontColorHex = NormalizeHexColor(nameFontColorHex, fallbackColor);
        template.UpdatedDate = now;
        template.UpdatedBy = actor;

        if (isDefault)
        {
            foreach (ParticipationCertificateTemplate item in templates)
            {
                item.IsDefault = item.Id == template.Id;
                item.UpdatedDate = now;
                item.UpdatedBy = actor;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ParticipationCertificateOperationResult> GenerateAsync(
        Guid congressId,
        string? certificateCulture,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        ParticipationCertificateGenerationJobDto job = await QueueGenerationAsync(
            new ParticipationCertificateGenerationQueueInput
            {
                CongressId = congressId,
                CertificateCulture = certificateCulture,
                SelectAllFiltered = true,
                RequestedByUserId = performedByUserId
            },
            cancellationToken);

        return new ParticipationCertificateOperationResult
        {
            JobId = job.Id,
            CandidateCount = job.TotalCount,
            SkippedCount = job.ExcludedCount,
            Warnings = new[] { "Belge üretimi arka plan kuyruğuna alındı." }
        };
    }

    public async Task<ParticipationCertificateOperationResult> QueueEmailsAsync(
        Guid congressId,
        IReadOnlyCollection<Guid> certificateIds,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        List<Guid> selectedIds = certificateIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
            throw new InvalidOperationException("Mail gönderimi için en az bir katılım belgesi seçmelisiniz.");

        string? certificateCulture = await _context.ParticipationCertificates
            .AsNoTracking()
            .Where(certificate =>
                certificate.CongressId == congressId &&
                certificate.DeletedDate == null &&
                selectedIds.Contains(certificate.Id))
            .Select(certificate => certificate.Culture)
            .FirstOrDefaultAsync(cancellationToken);

        return await RequestEmailQueueAsync(
            new ParticipationCertificateEmailQueueInput
            {
                CongressId = congressId,
                CertificateCulture = certificateCulture,
                CertificateIds = selectedIds,
                SelectAllFiltered = false,
                RequestedByUserId = performedByUserId
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<ParticipationCertificateCandidateDto>> BuildCandidatesAsync(
        Guid congressId,
        string? displayCulture,
        string? certificateCulture,
        ParticipationCertificateDashboardFilter? filter,
        CancellationToken cancellationToken)
    {
        ParticipationCertificateDashboardFilter resolvedFilter = filter ?? GetDefaultEligibilityFilter();
        string resolvedCertificateCulture = ParticipationCertificateCultures.Normalize(certificateCulture);
        LanguageSelection displayLanguage = await ResolveLanguageSelectionAsync(displayCulture, cancellationToken);
        LanguageSelection certificateLanguage = await ResolveLanguageSelectionAsync(resolvedCertificateCulture, cancellationToken);

        HashSet<Guid> timedProgramSubmissionIds = await _context.CongressProgramItems
            .AsNoTracking()
            .Where(item =>
                item.DeletedDate == null &&
                item.ProgramSession.DeletedDate == null &&
                item.ProgramSession.ProgramDay.DeletedDate == null &&
                item.ProgramSession.ProgramDay.ProgramPlan.DeletedDate == null &&
                item.ProgramSession.ProgramDay.ProgramPlan.CongressId == congressId)
            .Select(item => item.SubmissionId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        HashSet<Guid> videoSubmissionIds = await _context.SubmissionFiles
            .AsNoTracking()
            .Where(file =>
                file.DeletedDate == null &&
                file.IsActive &&
                file.FileKind == SubmissionFileKind.Presentation &&
                file.ReviewStatus == SubmissionFileReviewStatus.Approved &&
                file.IsIncludedInProgramBook &&
                file.Submission.DeletedDate == null &&
                file.Submission.CongressId == congressId)
            .Select(file => file.SubmissionId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        HashSet<Guid> includedSubmissionIds = timedProgramSubmissionIds
            .Concat(videoSubmissionIds)
            .Distinct()
            .ToHashSet();

        if (includedSubmissionIds.Count == 0)
            return Array.Empty<ParticipationCertificateCandidateDto>();

        List<Symplify.BackOffice.Domain.Submission.Submission> submissions = await _context.Submissions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Authors)
                .ThenInclude(author => author.Title)
                    .ThenInclude(title => title!.Translations)
            .Include(item => item.TransactionStatus)
                .ThenInclude(status => status!.Translations)
            .Include(item => item.PaymentStatus)
                .ThenInclude(status => status!.Translations)
            .Include(item => item.SubmissionType)
                .ThenInclude(type => type!.Translations)
            .Where(item =>
                item.DeletedDate == null &&
                item.CongressId == congressId &&
                includedSubmissionIds.Contains(item.Id))
            .OrderBy(item => item.SubmissionNumber)
            .ToListAsync(cancellationToken);

        List<ParticipationCertificate> existingCertificates = await _context.ParticipationCertificates
            .AsNoTracking()
            .Where(item =>
                item.CongressId == congressId &&
                item.DeletedDate == null)
            .ToListAsync(cancellationToken);

        Dictionary<(Guid SubmissionId, Guid AuthorId, string Culture), ParticipationCertificate> existingMap = existingCertificates
            .GroupBy(item => (
                item.SubmissionId,
                item.AuthorId,
                ParticipationCertificateCultures.Normalize(item.Culture)))
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(x => x.GeneratedAt).First());

        List<ParticipationCertificateCandidateDto> candidates = new();

        foreach (Symplify.BackOffice.Domain.Submission.Submission submission in submissions)
        {
            string? submissionStatusCode = submission.TransactionStatus?.Code;
            string? paymentStatusCode = submission.PaymentStatus?.Code;

            if (HasCodeFilter(resolvedFilter.SubmissionStatusCode) &&
                !MatchesSubmissionStatusFilter(submissionStatusCode, resolvedFilter.SubmissionStatusCode))
            {
                continue;
            }

            if (HasCodeFilter(resolvedFilter.PaymentStatusCode) &&
                !MatchesPaymentStatusFilter(paymentStatusCode, resolvedFilter.PaymentStatusCode))
            {
                continue;
            }

            bool isAccepted = IsAccepted(submissionStatusCode);
            bool isPaid = IsPaid(paymentStatusCode);
            bool isEligible = isAccepted && isPaid;
            bool isVideo = videoSubmissionIds.Contains(submission.Id) && !timedProgramSubmissionIds.Contains(submission.Id);

            foreach (Author author in submission.Authors.Where(author => author.DeletedDate == null))
            {
                existingMap.TryGetValue(
                    (submission.Id, author.Id, ParticipationCertificateCultures.Turkish),
                    out ParticipationCertificate? turkishCertificate);
                existingMap.TryGetValue(
                    (submission.Id, author.Id, ParticipationCertificateCultures.English),
                    out ParticipationCertificate? englishCertificate);

                candidates.Add(new ParticipationCertificateCandidateDto
                {
                    SubmissionId = submission.Id,
                    AuthorId = author.Id,
                    SubmissionNumber = submission.SubmissionNumber,
                    SubmissionTitle = submission.Title,
                    SubmissionTypeName = ResolveSubmissionTypeName(
                        submission.SubmissionType,
                        certificateLanguage.LanguageId,
                        certificateLanguage.DefaultLanguageId),
                    AuthorFullName = BuildAuthorName(author),
                    AuthorDisplayNameWithTitle = BuildAuthorFullName(
                        author,
                        certificateLanguage.LanguageId,
                        certificateLanguage.DefaultLanguageId),
                    AcademicTitle = ResolveTitle(
                        author,
                        certificateLanguage.LanguageId,
                        certificateLanguage.DefaultLanguageId),
                    AuthorEmail = author.Email,
                    AuthorInstitution = author.Institution,
                    SubmissionStatusCode = submissionStatusCode,
                    SubmissionStatusName = ResolveTransactionStatusName(
                        submission.TransactionStatus,
                        displayLanguage.LanguageId,
                        displayLanguage.DefaultLanguageId),
                    PaymentStatusCode = paymentStatusCode,
                    PaymentStatusName = ResolvePaymentStatusName(
                        submission.PaymentStatus,
                        displayLanguage.LanguageId,
                        displayLanguage.DefaultLanguageId),
                    IsEligible = isEligible,
                    IsVideoPresentation = isVideo,
                    TurkishCertificateId = turkishCertificate?.Id,
                    EnglishCertificateId = englishCertificate?.Id
                });
            }
        }

        return candidates
            .OrderByDescending(item => item.IsEligible)
            .ThenBy(item => item.SubmissionNumber)
            .ThenBy(item => item.AuthorFullName)
            .ToList();
    }

    private async Task<ParticipationCertificateSigner> ResolveCommitteeSignerAsync(
        Guid congressId,
        string certificateCulture,
        CancellationToken cancellationToken)
    {
        LanguageSelection language = await ResolveLanguageSelectionAsync(certificateCulture, cancellationToken);

        List<CongressBoard> boards = await _context.CongressBoards
            .AsNoTracking()
            .AsSplitQuery()
            .Include(board => board.Translations)
            .Include(board => board.Members)
                .ThenInclude(member => member.Translations)
            .Where(board => board.CongressId == congressId && board.DeletedDate == null)
            .OrderBy(board => board.Order <= 0 ? int.MaxValue : board.Order)
            .ThenBy(board => board.Id)
            .ToListAsync(cancellationToken);

        if (boards.Count == 0)
            throw new InvalidOperationException("Katılım belgesi imzası için kongre kurulu bulunamadı.");

        List<CongressBoard> organizingBoards = boards
            .Where(board => board.IsActive && IsOrganizingBoard(board))
            .ToList();

        if (organizingBoards.Count == 0)
        {
            throw new InvalidOperationException(
                "Katılım belgesi imzası için aktif Düzenleme Kurulu bulunamadı. Kongre Yönetimi > Kurullar bölümünde Düzenleme Kurulu oluşturun.");
        }

        List<ParticipationSignerCandidate> candidates = organizingBoards
            .SelectMany(board => board.Members
                .Where(member => member.IsActive && member.DeletedDate == null)
                .Select(member => new ParticipationSignerCandidate(
                    board,
                    member,
                    ResolveBoardName(board, language.LanguageId, language.DefaultLanguageId))))
            .ToList();

        ParticipationSignerCandidate? selected = candidates
            .Where(candidate => HasSignature(candidate.Member))
            .OrderByDescending(candidate => candidate.Member.IsAcceptanceLetterSigner)
            .ThenBy(candidate => candidate.Board.Order <= 0 ? int.MaxValue : candidate.Board.Order)
            .ThenBy(candidate => candidate.Member.Order <= 0 ? int.MaxValue : candidate.Member.Order)
            .ThenBy(candidate => candidate.Member.FullName)
            .FirstOrDefault();

        if (selected is null)
        {
            throw new InvalidOperationException(
                "Katılım belgesi için Düzenleme Kurulu içinde imza görseli bulunan üye bulunamadı. Düzenleme Kurulu başkanını imza yetkilisi olarak işaretleyip imza görselini yükleyin.");
        }

        string? objectName = FirstNonEmpty(selected.Member.SignatureObjectName, selected.Member.SignaturePath);
        if (string.IsNullOrWhiteSpace(objectName))
            throw new InvalidOperationException("Katılım belgesi imza görseli bulunamadı.");

        string bucketName = FirstNonEmpty(selected.Member.SignatureBucketName, _storageOptions.Buckets.CongressImages);
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Katılım belgesi imza bucket bilgisi bulunamadı.");

        List<Symplify.BackOffice.Domain.Lookups.Title> academicTitles = await _context.Titles
            .AsNoTracking()
            .AsSplitQuery()
            .Include(title => title.Translations)
            .Where(title => title.DeletedDate == null && title.IsActive)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<string, string> titleShortNameLookup = BuildTitleShortNameLookup(
            academicTitles,
            language.LanguageId,
            language.DefaultLanguageId);

        return new ParticipationCertificateSigner(
            selected.Member.Id,
            ResolveMemberFullName(selected.Member, language.LanguageId, language.DefaultLanguageId),
            ResolveMemberAcademicTitle(
                selected.Member,
                titleShortNameLookup,
                language.LanguageId,
                language.DefaultLanguageId),
            ResolveMemberRoleTitle(selected.BoardName, certificateCulture),
            selected.BoardName,
            bucketName,
            objectName);
    }


    private async Task<IReadOnlyList<ParticipationCertificateFilterOptionDto>> BuildSubmissionStatusOptionsAsync(
        Guid congressId,
        string? culture,
        CancellationToken cancellationToken)
    {
        LanguageSelection language = await ResolveLanguageSelectionAsync(culture, cancellationToken);

        List<Symplify.BackOffice.Domain.Submission.Submission> submissions = await _context.Submissions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.TransactionStatus)
                .ThenInclude(status => status!.Translations)
            .Where(item => item.DeletedDate == null && item.CongressId == congressId && item.TransactionStatusId != null)
            .ToListAsync(cancellationToken);

        return submissions
            .Where(item => item.TransactionStatus is not null && !string.IsNullOrWhiteSpace(item.TransactionStatus.Code))
            .GroupBy(item => NormalizeCode(item.TransactionStatus!.Code), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                Symplify.BackOffice.Domain.Workflow.TransactionStatus status = group.First().TransactionStatus!;
                return new
                {
                    status.Order,
                    Dto = new ParticipationCertificateFilterOptionDto
                    {
                        Code = status.Code,
                        Text = ResolveTransactionStatusName(status, language.LanguageId, language.DefaultLanguageId),
                        Count = group.Count()
                    }
                };
            })
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Dto.Text)
            .Select(item => item.Dto)
            .ToList();
    }

    private async Task<IReadOnlyList<ParticipationCertificateFilterOptionDto>> BuildPaymentStatusOptionsAsync(
        Guid congressId,
        string? culture,
        CancellationToken cancellationToken)
    {
        LanguageSelection language = await ResolveLanguageSelectionAsync(culture, cancellationToken);

        List<Symplify.BackOffice.Domain.Submission.Submission> submissions = await _context.Submissions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.PaymentStatus)
                .ThenInclude(status => status!.Translations)
            .Where(item => item.DeletedDate == null && item.CongressId == congressId && item.PaymentStatusId != null)
            .ToListAsync(cancellationToken);

        return submissions
            .Where(item => item.PaymentStatus is not null && !string.IsNullOrWhiteSpace(item.PaymentStatus.Code))
            .GroupBy(item => NormalizeCode(item.PaymentStatus!.Code), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                Symplify.BackOffice.Domain.Workflow.PaymentStatus status = group.First().PaymentStatus!;
                return new
                {
                    status.Order,
                    Dto = new ParticipationCertificateFilterOptionDto
                    {
                        Code = status.Code,
                        Text = ResolvePaymentStatusName(status, language.LanguageId, language.DefaultLanguageId),
                        Count = group.Count()
                    }
                };
            })
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Dto.Text)
            .Select(item => item.Dto)
            .ToList();
    }

    private async Task<LanguageSelection> ResolveLanguageSelectionAsync(string? culture, CancellationToken cancellationToken)
    {
        string normalizedCulture = string.IsNullOrWhiteSpace(culture) ? "tr-TR" : culture.Trim();

        Guid? languageId = await _context.Languages
            .AsNoTracking()
            .Where(item => item.DeletedDate == null && item.IsActive && item.Culture == normalizedCulture)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Guid? defaultLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(item => item.DeletedDate == null && item.IsActive && item.IsDefault)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new LanguageSelection(languageId, defaultLanguageId);
    }

    private static string ResolveTransactionStatusName(
        Symplify.BackOffice.Domain.Workflow.TransactionStatus? status,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        if (status is null)
            return "-";

        string? name = null;

        if (languageId.HasValue)
            name = status.Translations.FirstOrDefault(item => item.DeletedDate == null && item.LanguageId == languageId.Value)?.Name;

        if (string.IsNullOrWhiteSpace(name) && defaultLanguageId.HasValue)
            name = status.Translations.FirstOrDefault(item => item.DeletedDate == null && item.LanguageId == defaultLanguageId.Value)?.Name;

        if (string.IsNullOrWhiteSpace(name))
            name = status.Translations.FirstOrDefault(item => item.DeletedDate == null)?.Name;

        return FirstNonEmpty(name, status.Code, "-");
    }

    private static string ResolvePaymentStatusName(
        Symplify.BackOffice.Domain.Workflow.PaymentStatus? status,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        if (status is null)
            return "-";

        string? name = null;

        if (languageId.HasValue)
            name = status.Translations.FirstOrDefault(item => item.DeletedDate == null && item.LanguageId == languageId.Value)?.Name;

        if (string.IsNullOrWhiteSpace(name) && defaultLanguageId.HasValue)
            name = status.Translations.FirstOrDefault(item => item.DeletedDate == null && item.LanguageId == defaultLanguageId.Value)?.Name;

        if (string.IsNullOrWhiteSpace(name))
            name = status.Translations.FirstOrDefault(item => item.DeletedDate == null)?.Name;

        return FirstNonEmpty(name, status.Code, "-");
    }

    private static string BuildCongressOptionText(Congress congress, Guid? languageId, Guid? defaultLanguageId)
    {
        string? title = null;

        if (languageId.HasValue)
            title = congress.Translations.FirstOrDefault(item => item.DeletedDate == null && item.LanguageId == languageId.Value)?.Title;

        if (string.IsNullOrWhiteSpace(title) && defaultLanguageId.HasValue)
            title = congress.Translations.FirstOrDefault(item => item.DeletedDate == null && item.LanguageId == defaultLanguageId.Value)?.Title;

        if (string.IsNullOrWhiteSpace(title))
            title = congress.Translations.FirstOrDefault(item => item.DeletedDate == null)?.Title;

        string displayTitle = FirstNonEmpty(title, congress.Name, congress.Code, "Kongre");
        return string.IsNullOrWhiteSpace(congress.Code) ? displayTitle : $"{displayTitle} ({congress.Code})";
    }

    private static string ResolveMemberFullName(
        CongressBoardMember member,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        CongressBoardMemberTranslation? selected =
            member.Translations.FirstOrDefault(translation =>
                translation.DeletedDate == null &&
                languageId.HasValue &&
                translation.LanguageId == languageId.Value)
            ?? member.Translations.FirstOrDefault(translation =>
                translation.DeletedDate == null &&
                defaultLanguageId.HasValue &&
                translation.LanguageId == defaultLanguageId.Value)
            ?? member.Translations
                .Where(translation => translation.DeletedDate == null)
                .OrderBy(translation => translation.CreatedDate)
                .FirstOrDefault();

        return FirstNonEmpty(selected?.FullName, member.FullName);
    }

    private static string ResolveMemberRoleTitle(string boardName, string certificateCulture)
    {
        bool english = string.Equals(
            ParticipationCertificateCultures.Normalize(certificateCulture),
            ParticipationCertificateCultures.English,
            StringComparison.OrdinalIgnoreCase);

        if (IsOrganizingBoardName(boardName))
            return english ? "Head of Organizing Committee" : "Düzenleme Kurulu Başkanı";

        return FirstNonEmpty(
            boardName,
            english ? "Head of Organizing Committee" : "Düzenleme Kurulu Başkanı");
    }

    private static IReadOnlyDictionary<string, string> BuildTitleShortNameLookup(
        IEnumerable<Symplify.BackOffice.Domain.Lookups.Title> titles,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        Dictionary<string, string> lookup = new(StringComparer.OrdinalIgnoreCase);

        foreach (Symplify.BackOffice.Domain.Lookups.Title title in titles)
        {
            List<Symplify.BackOffice.Domain.Lookups.TitleTranslation> translations = title.Translations
                .Where(translation => translation.DeletedDate == null)
                .ToList();

            Symplify.BackOffice.Domain.Lookups.TitleTranslation? selectedTranslation =
                translations.FirstOrDefault(translation => languageId.HasValue && translation.LanguageId == languageId.Value)
                ?? translations.FirstOrDefault(translation => defaultLanguageId.HasValue && translation.LanguageId == defaultLanguageId.Value)
                ?? translations.OrderBy(translation => translation.CreatedDate).FirstOrDefault();

            string displayValue = FirstNonEmpty(
                selectedTranslation?.Description,
                selectedTranslation?.Name);

            if (string.IsNullOrWhiteSpace(displayValue))
                continue;

            AddTitleLookupKey(lookup, title.Code, displayValue);

            foreach (Symplify.BackOffice.Domain.Lookups.TitleTranslation translation in translations)
            {
                AddTitleLookupKey(lookup, translation.Name, displayValue);
                AddTitleLookupKey(lookup, translation.Description, displayValue);
            }
        }

        return lookup;
    }

    private static string ResolveMemberAcademicTitle(
        CongressBoardMember member,
        IReadOnlyDictionary<string, string> titleShortNameLookup,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        CongressBoardMemberTranslation? selectedMemberTranslation =
            member.Translations.FirstOrDefault(translation =>
                translation.DeletedDate == null &&
                languageId.HasValue &&
                translation.LanguageId == languageId.Value)
            ?? member.Translations.FirstOrDefault(translation =>
                translation.DeletedDate == null &&
                defaultLanguageId.HasValue &&
                translation.LanguageId == defaultLanguageId.Value);

        IEnumerable<string?> candidates = new[]
            {
                selectedMemberTranslation?.Title,
                member.AcademicTitle
            }
            .Concat(member.Translations
                .Where(translation => translation.DeletedDate == null)
                .OrderBy(translation => translation.CreatedDate)
                .Select(translation => translation.Title));

        foreach (string? candidate in candidates)
        {
            string key = NormalizeTextForSearch(candidate);
            if (!string.IsNullOrWhiteSpace(key) &&
                titleShortNameLookup.TryGetValue(key, out string? shortName) &&
                !string.IsNullOrWhiteSpace(shortName))
            {
                return shortName.Trim();
            }
        }

        return FirstNonEmpty(selectedMemberTranslation?.Title, member.AcademicTitle);
    }

    private static void AddTitleLookupKey(
        IDictionary<string, string> lookup,
        string? value,
        string displayValue)
    {
        string key = NormalizeTextForSearch(value);
        if (!string.IsNullOrWhiteSpace(key))
            lookup[key] = displayValue;
    }

    private static string ResolveBoardName(
        CongressBoard board,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        CongressBoardTranslation? selected =
            board.Translations.FirstOrDefault(translation =>
                translation.DeletedDate == null &&
                languageId.HasValue &&
                translation.LanguageId == languageId.Value)
            ?? board.Translations.FirstOrDefault(translation =>
                translation.DeletedDate == null &&
                defaultLanguageId.HasValue &&
                translation.LanguageId == defaultLanguageId.Value)
            ?? board.Translations
                .Where(translation => translation.DeletedDate == null)
                .OrderBy(translation => translation.CreatedDate)
                .FirstOrDefault();

        return FirstNonEmpty(selected?.Name);
    }

    private static bool IsOrganizingBoard(CongressBoard board)
        => board.Translations
            .Where(translation => translation.DeletedDate == null)
            .Select(translation => translation.Name)
            .Any(IsOrganizingBoardName);

    private static bool IsOrganizingBoardName(string? value)
    {
        string normalized = NormalizeTextForSearch(value);
        return normalized.Contains("duzenleme", StringComparison.Ordinal) ||
               normalized.Contains("organizing", StringComparison.Ordinal) ||
               normalized.Contains("organisation", StringComparison.Ordinal) ||
               normalized.Contains("organization", StringComparison.Ordinal) ||
               normalized.Contains("editorial", StringComparison.Ordinal);
    }

    private static bool HasSignature(CongressBoardMember member)
        => !string.IsNullOrWhiteSpace(member.SignatureObjectName) || !string.IsNullOrWhiteSpace(member.SignaturePath);

    private async Task<CongressInfo> ResolveCongressAsync(Guid congressId, string? culture, CancellationToken cancellationToken)
    {
        string normalizedCulture = string.IsNullOrWhiteSpace(culture) ? "tr-TR" : culture.Trim();
        Guid? languageId = await _context.Languages
            .AsNoTracking()
            .Where(item => item.DeletedDate == null && item.IsActive && item.Culture == normalizedCulture)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Guid? defaultLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(item => item.DeletedDate == null && item.IsActive && item.IsDefault)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var congress = await _context.Congresses
            .AsNoTracking()
            .Include(item => item.Translations)
            .Where(item => item.Id == congressId && item.DeletedDate == null)
            .Select(item => new
            {
                item.Id,
                item.OrganizationId,
                item.Name,
                item.Code,
                Translations = item.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new
                    {
                        translation.LanguageId,
                        translation.Title
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (congress is null)
            throw new InvalidOperationException("Kongre bulunamadı.");

        string? title = null;
        if (languageId.HasValue)
            title = congress.Translations.FirstOrDefault(item => item.LanguageId == languageId.Value)?.Title;
        if (string.IsNullOrWhiteSpace(title) && defaultLanguageId.HasValue)
            title = congress.Translations.FirstOrDefault(item => item.LanguageId == defaultLanguageId.Value)?.Title;
        if (string.IsNullOrWhiteSpace(title))
            title = congress.Translations.FirstOrDefault()?.Title;

        return new CongressInfo(congress.Id, congress.OrganizationId, FirstNonEmpty(title, congress.Name, congress.Code, "Kongre"), normalizedCulture);
    }

    private async Task EnsureCongressExistsAsync(Guid congressId, CancellationToken cancellationToken)
    {
        bool exists = await _context.Congresses
            .AsNoTracking()
            .AnyAsync(item => item.Id == congressId && item.DeletedDate == null, cancellationToken);

        if (!exists)
            throw new InvalidOperationException("Kongre bulunamadı.");
    }

    private async Task<IReadOnlyList<ParticipationCertificateTemplate>> GetActiveTemplatesAsync(
        Guid congressId,
        CancellationToken cancellationToken)
    {
        return await _context.ParticipationCertificateTemplates
            .Where(item => item.CongressId == congressId && item.IsActive && item.DeletedDate == null)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.Culture)
            .ThenByDescending(item => item.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    private static string ResolveDashboardCertificateCulture(
        string? requestedCulture,
        IReadOnlyList<ParticipationCertificateTemplate> templates)
    {
        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            string normalizedRequested = ParticipationCertificateCultures.Normalize(requestedCulture);

            // Dashboard/template configuration must honor a supported requested culture
            // even when that culture does not have a template yet. Otherwise selecting
            // English falls back to the existing Turkish template and the upload form
            // posts tr-TR again, making the first English template impossible to create.
            if (ParticipationCertificateCultures.IsSupported(normalizedRequested))
                return normalizedRequested;
        }

        return templates.FirstOrDefault(item => item.IsDefault)?.Culture
            ?? templates.FirstOrDefault()?.Culture
            ?? ParticipationCertificateCultures.Turkish;
    }

    private static string ResolveGenerationCertificateCulture(
        string? requestedCulture,
        IReadOnlyList<ParticipationCertificateTemplate> templates)
    {
        if (templates.Count == 0)
            throw new InvalidOperationException("Bu kongre için aktif katılım sertifikası template PDF dosyası yok.");

        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            string normalizedRequested = ParticipationCertificateCultures.Normalize(requestedCulture);
            if (!ParticipationCertificateCultures.IsSupported(normalizedRequested))
                throw new InvalidOperationException("Katılım sertifikası dili yalnızca Türkçe veya İngilizce olabilir.");

            if (!templates.Any(item => string.Equals(item.Culture, normalizedRequested, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"{ParticipationCertificateCultures.GetDisplayName(normalizedRequested)} katılım sertifikası template PDF dosyası yüklenmemiş.");
            }

            return normalizedRequested;
        }

        return templates.FirstOrDefault(item => item.IsDefault)?.Culture
            ?? templates.First().Culture;
    }

    private async Task<byte[]> ReadRequiredObjectBytesAsync(
        string bucketName,
        string objectName,
        string notFoundMessage,
        string accessErrorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            bool exists = await _objectStorageService.ExistsAsync(bucketName, objectName, cancellationToken);
            if (!exists)
                throw new InvalidOperationException(notFoundMessage);

            await using Stream stream = await _objectStorageService.OpenReadAsync(bucketName, objectName, cancellationToken);
            using MemoryStream memoryStream = new();
            await stream.CopyToAsync(memoryStream, cancellationToken);

            byte[] bytes = memoryStream.ToArray();
            if (bytes.Length == 0)
                throw new InvalidOperationException(notFoundMessage);

            return bytes;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(accessErrorMessage, exception);
        }
    }

    private string GetSubmissionsBucketName()
    {
        if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.Submissions))
            throw new InvalidOperationException("ObjectStorage:Buckets:Submissions ayarı eksik.");

        return _storageOptions.Buckets.Submissions.Trim();
    }

    private static ParticipationCertificateTemplateDto MapTemplate(ParticipationCertificateTemplate template)
    {
        return new ParticipationCertificateTemplateDto
        {
            Id = template.Id,
            FileName = template.FileName,
            UploadedAt = template.UploadedAt,
            Culture = template.Culture,
            IsDefault = template.IsDefault,
            BodyText = template.BodyText,
            MailSubject = template.MailSubject,
            MailTitle = template.MailTitle,
            MailBodyHtml = template.MailBodyHtml,
            NameBoxX = template.NameBoxX,
            NameBoxY = template.NameBoxY,
            NameBoxWidth = template.NameBoxWidth,
            NameBoxHeight = template.NameBoxHeight,
            NameFontSize = template.NameFontSize,
            NameFontColorHex = template.NameFontColorHex,
            CoverPlaceholderBackground = template.CoverPlaceholderBackground,
            RenderCommitteeSignature = template.RenderCommitteeSignature,
            CommitteeSignatureBoxX = template.CommitteeSignatureBoxX,
            CommitteeSignatureBoxY = template.CommitteeSignatureBoxY,
            CommitteeSignatureBoxWidth = template.CommitteeSignatureBoxWidth,
            CommitteeSignatureBoxHeight = template.CommitteeSignatureBoxHeight
        };
    }

    private static bool IsAccepted(string? code)
        => AcceptedStatusCodes.Contains(NormalizeCode(code));

    private static bool IsPaid(string? code)
        => PaidPaymentStatusCodes.Contains(NormalizeCode(code));

    private static bool MatchesSubmissionStatusFilter(string? actualCode, string? requestedCode)
    {
        if (IsAccepted(requestedCode))
            return IsAccepted(actualCode);

        return CodesEqual(actualCode, requestedCode);
    }

    private static bool MatchesPaymentStatusFilter(string? actualCode, string? requestedCode)
    {
        if (IsPaid(requestedCode))
            return IsPaid(actualCode);

        return CodesEqual(actualCode, requestedCode);
    }

    private static ParticipationCertificateDashboardFilter GetDefaultEligibilityFilter()
        => new()
        {
            SubmissionStatusCode = "ACCEPTED",
            PaymentStatusCode = "PAYMENT_COMPLETED"
        };

    private static bool HasCodeFilter(string? code)
        => !string.IsNullOrWhiteSpace(code);

    private static bool CodesEqual(string? left, string? right)
        => string.Equals(NormalizeCode(left), NormalizeCode(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return new string(code.Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    private static string BuildAuthorName(Author author)
    {
        return string.Join(' ', new[] { author.FirstName, author.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()));
    }

    private static string BuildAuthorFullName(Author author, Guid? languageId, Guid? defaultLanguageId)
    {
        string title = ResolveTitle(author, languageId, defaultLanguageId);
        string name = BuildAuthorName(author);

        return string.IsNullOrWhiteSpace(title) ? name : $"{title} {name}";
    }

    private static string NormalizeHexColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        string normalized = value.Trim();
        if (!normalized.StartsWith('#'))
            normalized = $"#{normalized}";

        if (normalized.Length != 7 ||
            !normalized.Skip(1).All(character => Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException("Yazı rengi #RRGGBB formatında olmalıdır.");
        }

        return normalized.ToUpperInvariant();
    }


    private static string NormalizeMailSubject(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > 300)
            throw new InvalidOperationException("Mail konusu en fazla 300 karakter olabilir.");
        return normalized;
    }

    private static string NormalizeMailTitle(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > 300)
            throw new InvalidOperationException("Mail başlığı en fazla 300 karakter olabilir.");
        return normalized;
    }

    private static string NormalizeMailBodyHtml(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > 50000)
            throw new InvalidOperationException("Mail içeriği en fazla 50.000 karakter olabilir.");
        return normalized;
    }

    private static void ValidateMailTemplate(string subject, string bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new InvalidOperationException("Mail konusu zorunludur.");

        if (string.IsNullOrWhiteSpace(bodyHtml))
            throw new InvalidOperationException("Mail içeriği zorunludur.");

        if (!bodyHtml.Contains("{{CERTIFICATE_LINK}}", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Mail içeriğinde {{CERTIFICATE_LINK}} değişkeni bulunmalıdır. Belge PDF olarak eklenmeyecek, güvenli public link ile gönderilecektir.");
        }
    }

    private static string NormalizeCertificateBodyText(string? bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText))
            return string.Empty;

        string normalized = bodyText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();

        return normalized;
    }

    private static string BuildCertificateText(string bodyTextTemplate, string? submissionTypeName)
    {
        string submissionType = FirstNonEmpty(submissionTypeName, "Presentation");

        return bodyTextTemplate
            .Replace("{{SUBMISSION_TYPE}}", submissionType, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PRESENTATION_TYPE}}", submissionType, StringComparison.OrdinalIgnoreCase)
            .Replace("{{BILDIRI_TURU}}", submissionType, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string ResolveSubmissionTypeName(
        Symplify.BackOffice.Domain.Lookups.SubmissionType? submissionType,
        Guid? languageId,
        Guid? defaultLanguageId)
    {
        if (submissionType is null)
            return string.Empty;

        List<Symplify.BackOffice.Domain.Lookups.SubmissionTypeTranslation> translations =
            submissionType.Translations?
                .Where(translation => translation.DeletedDate == null)
                .ToList()
            ?? new List<Symplify.BackOffice.Domain.Lookups.SubmissionTypeTranslation>();

        Symplify.BackOffice.Domain.Lookups.SubmissionTypeTranslation? requestedTranslation = translations
            .FirstOrDefault(translation =>
                languageId.HasValue &&
                translation.LanguageId == languageId.Value);

        Symplify.BackOffice.Domain.Lookups.SubmissionTypeTranslation? defaultTranslation = translations
            .FirstOrDefault(translation =>
                defaultLanguageId.HasValue &&
                translation.LanguageId == defaultLanguageId.Value);

        Symplify.BackOffice.Domain.Lookups.SubmissionTypeTranslation? anyTranslation = translations
            .OrderBy(translation => translation.CreatedDate)
            .FirstOrDefault();

        // Sertifika dili önceliklidir. İlgili çeviri yoksa kod, ardından güvenli fallback kullanılır.
        return FirstNonEmpty(
            requestedTranslation?.Name,
            requestedTranslation?.Description,
            submissionType.Code,
            defaultTranslation?.Name,
            defaultTranslation?.Description,
            anyTranslation?.Name,
            anyTranslation?.Description);
    }

    private static string ResolveTitle(Author author, Guid? languageId, Guid? defaultLanguageId)
    {
        if (author.Title?.Translations is null || author.Title.Translations.Count == 0)
            return string.Empty;

        List<Symplify.BackOffice.Domain.Lookups.TitleTranslation> translations = author.Title.Translations
            .Where(translation => translation.DeletedDate == null)
            .ToList();

        Symplify.BackOffice.Domain.Lookups.TitleTranslation? requestedTranslation = translations
            .FirstOrDefault(translation =>
                languageId.HasValue &&
                translation.LanguageId == languageId.Value);

        Symplify.BackOffice.Domain.Lookups.TitleTranslation? defaultTranslation = translations
            .FirstOrDefault(translation =>
                defaultLanguageId.HasValue &&
                translation.LanguageId == defaultLanguageId.Value);

        Symplify.BackOffice.Domain.Lookups.TitleTranslation? anyTranslation = translations
            .OrderBy(translation => translation.CreatedDate)
            .FirstOrDefault();

        // TitleTranslations.Description kısa unvan alanıdır.
        // Kod içerisinde sabit akademik unvan eşleştirmesi yapılmaz.
        return FirstNonEmpty(
            requestedTranslation?.Description,
            requestedTranslation?.Name,
            defaultTranslation?.Description,
            defaultTranslation?.Name,
            anyTranslation?.Description,
            anyTranslation?.Name);
    }

    private static string BuildMailBodyHtml(
        ParticipationCertificate certificate,
        string congressTitle,
        string certificateCulture)
    {
        string safeName = Html(certificate.AuthorFullNameSnapshot);
        string safeCongress = Html(congressTitle);
        string safeSubmission = Html(certificate.SubmissionTitleSnapshot);
        string safeNumber = Html(certificate.SubmissionNumber);

        bool english = string.Equals(
            ParticipationCertificateCultures.Normalize(certificateCulture),
            ParticipationCertificateCultures.English,
            StringComparison.OrdinalIgnoreCase);

        if (english)
        {
            return $"""
<p>Dear <strong>{safeName}</strong>,</p>
<p>Thank you for your scientific contribution to <strong>{safeCongress}</strong>. Your personalized certificate of participation has been prepared and attached to this email as a PDF.</p>
<table role="presentation" style="width:100%;border-collapse:collapse;margin:18px 0;background:#f8fafc;border:1px solid #e5e7eb;border-radius:8px;">
    <tr>
        <td style="padding:10px 14px;width:150px;color:#64748b;font-size:13px;">Submission No</td>
        <td style="padding:10px 14px;font-weight:600;color:#0f172a;">{safeNumber}</td>
    </tr>
    <tr>
        <td style="padding:10px 14px;color:#64748b;font-size:13px;border-top:1px solid #e5e7eb;">Submission Title</td>
        <td style="padding:10px 14px;color:#0f172a;border-top:1px solid #e5e7eb;">{safeSubmission}</td>
    </tr>
</table>
<p>You can also download the document from <strong>My Submissions &gt; Files &amp; Documents</strong> in the system.</p>
<p>Kind regards,<br /><strong>{safeCongress}</strong></p>
""";
        }

        return $"""
<p>Sayın <strong>{safeName}</strong>,</p>
<p><strong>{safeCongress}</strong> kapsamında gerçekleştirdiğiniz bilimsel katkı için teşekkür ederiz. Kişiye özel katılım belgeniz hazırlanmış ve bu e-postaya PDF olarak eklenmiştir.</p>
<table role="presentation" style="width:100%;border-collapse:collapse;margin:18px 0;background:#f8fafc;border:1px solid #e5e7eb;border-radius:8px;">
    <tr>
        <td style="padding:10px 14px;width:150px;color:#64748b;font-size:13px;">Bildiri No</td>
        <td style="padding:10px 14px;font-weight:600;color:#0f172a;">{safeNumber}</td>
    </tr>
    <tr>
        <td style="padding:10px 14px;color:#64748b;font-size:13px;border-top:1px solid #e5e7eb;">Bildiri Başlığı</td>
        <td style="padding:10px 14px;color:#0f172a;border-top:1px solid #e5e7eb;">{safeSubmission}</td>
    </tr>
</table>
<p>Belgenizi ayrıca sistemde <strong>Bildirilerim &gt; Dosya &amp; Belgeler</strong> alanından da indirebilirsiniz.</p>
<p>Saygılarımızla,<br /><strong>{safeCongress}</strong></p>
""";
    }

    private static string Html(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string NormalizeFileNamePart(string? value)
    {
        string normalized = InvalidFileNameChars.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), "-");
        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(normalized.Trim('-')) ? "submission" : normalized.Trim('-');
    }

    private static string NormalizeTextForSearch(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : Slug(value);

    private static string Slug(string value)
    {
        string normalized = value.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder builder = new();

        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private sealed record CongressInfo(Guid Id, Guid OrganizationId, string Title, string Culture);

    private sealed record LanguageSelection(Guid? LanguageId, Guid? DefaultLanguageId);

    private sealed record ParticipationCertificateSigner(
        Guid MemberId,
        string FullName,
        string AcademicTitle,
        string RoleTitle,
        string BoardName,
        string SignatureBucketName,
        string SignatureObjectName);

    private sealed record ParticipationSignerCandidate(
        CongressBoard Board,
        CongressBoardMember Member,
        string BoardName);
}
