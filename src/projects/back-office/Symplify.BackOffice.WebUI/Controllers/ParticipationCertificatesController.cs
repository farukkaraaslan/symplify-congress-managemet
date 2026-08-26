using System.IO.Compression;
using System.Security.Claims;
using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;
using Symplify.BackOffice.WebUI.Models.ParticipationCertificates;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/participation-certificates")]
public sealed class ParticipationCertificatesController : Controller
{
    private const int MaxZipFileCount = 2000;
    private const int MaxSelectionCount = 50000;

    private readonly IParticipationCertificateService _service;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<ParticipationCertificatesController> _logger;

    public ParticipationCertificatesController(
        IParticipationCertificateService service,
        IObjectStorageService objectStorageService,
        ILogger<ParticipationCertificatesController> logger)
    {
        _service = service;
        _objectStorageService = objectStorageService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        Guid? congressId,
        string? certificateCulture,
        CancellationToken cancellationToken = default)
    {
        string culture = GetRouteCulture();
        // Kongre yönetim ekranından gelindiyse seçili kongre Archived/Draft olsa bile
        // seçeneklere dahil edilir. Sidebar/genel girişte ise yalnız Published kongreler listelenir.
        IReadOnlyList<ParticipationCertificateCongressOptionDto> congresses = await _service.GetCongressOptionsAsync(
            culture,
            congressId,
            cancellationToken);

        if (congresses.Count == 0)
        {
            TempData["ErrorMessage"] = "Katılım belgesi işlemi için uygun kongre bulunamadı.";
            return View(CreateEmptyModel(Array.Empty<ParticipationCertificateCongressOptionDto>()));
        }

        bool hasSelectedCongress = congressId.HasValue && congresses.Any(item => item.Id == congressId.Value);
        if (!hasSelectedCongress)
        {
            ParticipationCertificatesIndexViewModel emptyModel = CreateEmptyModel(congresses);
            return View(emptyModel);
        }

        Guid selectedCongressId = congressId!.Value;
        ParticipationCertificateDashboardDto dashboard = await _service.GetDashboardAsync(
            selectedCongressId,
            culture,
            new ParticipationCertificateDashboardFilter
            {
                CertificateCulture = certificateCulture
            },
            cancellationToken);

        ParticipationCertificatesIndexViewModel model = new()
        {
            CongressId = dashboard.CongressId,
            CongressTitle = dashboard.CongressTitle,
            CertificateCulture = dashboard.CertificateCulture,
            DefaultCertificateCulture = dashboard.DefaultCertificateCulture,
            CongressOptions = BuildCongressOptions(congresses, dashboard.CongressId),
            CertificateCultureOptions = BuildCertificateCultureOptions(dashboard.CertificateCulture),
            SubmissionStatusOptions = BuildFilterOptions(
                dashboard.SubmissionStatusOptions,
                null,
                "Tüm bildiri durumları"),
            PaymentStatusOptions = BuildFilterOptions(
                dashboard.PaymentStatusOptions,
                null,
                "Tüm ödeme durumları"),
            Template = dashboard.Template,
            Templates = dashboard.Templates,
            CandidateCount = dashboard.CandidateCount,
            EligibleCandidateCount = dashboard.EligibleCandidateCount,
            GeneratedCount = dashboard.GeneratedCount,
            EmailQueuedCount = dashboard.EmailQueuedCount,
            EmailSentCount = dashboard.EmailSentCount,
            RevokedCount = dashboard.RevokedCount,
            MissingEmailCount = dashboard.MissingEmailCount,
            MailSelectableCount = dashboard.MailSelectableCount,
            GenerationJob = dashboard.GenerationJob
        };

        return View(model);
    }

    [HttpPost("template/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate(
        [FromForm] Guid congressId,
        [FromForm] string? templateCulture,
        [FromForm] bool isDefault,
        [FromForm] string? fontColorHex,
        [FromForm] string? bodyText,
        [FromForm] string? mailSubject,
        [FromForm] string? mailTitle,
        [FromForm] string? mailBodyHtml,
        [FromForm] IFormFile? templateFile,
        CancellationToken cancellationToken)
    {
        string culture = GetRouteCulture();
        string resolvedTemplateCulture = ParticipationCertificateCultures.Normalize(templateCulture);

        try
        {
            if (congressId == Guid.Empty)
                throw new InvalidOperationException("Kongre seçimi zorunludur.");

            if (templateFile is { Length: > 0 })
            {
                await using Stream stream = templateFile.OpenReadStream();
                await _service.UploadTemplateAsync(
                    new ParticipationCertificateTemplateUploadInput
                    {
                        CongressId = congressId,
                        FileName = templateFile.FileName,
                        ContentType = templateFile.ContentType,
                        Length = templateFile.Length,
                        Content = stream,
                        Culture = resolvedTemplateCulture,
                        IsDefault = isDefault,
                        NameFontColorHex = fontColorHex,
                        BodyText = bodyText,
                        MailSubject = mailSubject,
                        MailTitle = mailTitle,
                        MailBodyHtml = mailBodyHtml,
                        RenderCommitteeSignature = true
                    },
                    cancellationToken);
            }
            else
            {
                await _service.SaveTemplateSettingsAsync(
                    congressId,
                    resolvedTemplateCulture,
                    bodyText,
                    fontColorHex,
                    mailSubject,
                    mailTitle,
                    mailBodyHtml,
                    isDefault,
                    GetCurrentUserId(),
                    cancellationToken);
            }

            TempData["SuccessMessage"] =
                $"{ParticipationCertificateCultures.GetDisplayName(resolvedTemplateCulture)} PDF ve mail template ayarları kaydedildi.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "Participation certificate template validation failed. CongressId: {CongressId}, Culture: {Culture}",
                congressId,
                resolvedTemplateCulture);
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (Exception exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            _logger.LogError(
                exception,
                "Participation certificate template save failed. CongressId: {CongressId}, Culture: {Culture}, TraceId: {TraceId}",
                congressId,
                resolvedTemplateCulture,
                traceId);
            TempData["ErrorMessage"] = $"Template ayarları kaydedilemedi. Hata kodu: {traceId}";
        }

        return RedirectToAction(nameof(Index), new
        {
            culture,
            congressId,
            certificateCulture = resolvedTemplateCulture
        });
    }

    [HttpPost("template/default")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultTemplate(
        [FromForm] Guid congressId,
        [FromForm] string? templateCulture,
        CancellationToken cancellationToken)
    {
        string culture = GetRouteCulture();

        try
        {
            await _service.SetDefaultTemplateAsync(
                congressId,
                templateCulture,
                GetCurrentUserId(),
                cancellationToken);
            TempData["SuccessMessage"] =
                $"{ParticipationCertificateCultures.GetDisplayName(templateCulture)} template varsayılan yapıldı.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            culture,
            congressId,
            certificateCulture = ParticipationCertificateCultures.Normalize(templateCulture)
        });
    }

    [HttpPost("candidates/data")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CandidateData(
        [FromForm] DataTableRequest table,
        [FromForm] Guid congressId,
        [FromForm] string? submissionStatusCode,
        [FromForm] string? paymentStatusCode,
        CancellationToken cancellationToken)
    {
        string? sortColumn = ResolveSortColumn(table);
        string? sortDirection = table.Order.FirstOrDefault()?.Dir;

        ParticipationCertificateCandidatePageResult result = await _service.GetCandidatePageAsync(
            new ParticipationCertificateCandidatePageRequest
            {
                CongressId = congressId,
                DisplayCulture = GetRouteCulture(),
                SubmissionStatusCode = NormalizeEmpty(submissionStatusCode),
                PaymentStatusCode = NormalizeEmpty(paymentStatusCode),
                SearchText = NormalizeEmpty(table.Search?.Value),
                Start = Math.Max(0, table.Start),
                Length = Math.Clamp(table.Length <= 0 ? 25 : table.Length, 10, 250),
                SortColumn = sortColumn,
                SortDirection = sortDirection
            },
            cancellationToken);

        return Json(new
        {
            draw = table.Draw,
            recordsTotal = result.TotalCount,
            recordsFiltered = result.FilteredCount,
            data = result.Items
        });
    }

    [HttpPost("documents/data")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DocumentData(
        [FromForm] DataTableRequest table,
        [FromForm] Guid congressId,
        [FromForm] string? certificateCulture,
        [FromForm] string? emailStatus,
        [FromForm] bool includeRevoked,
        CancellationToken cancellationToken)
    {
        ParticipationCertificateDocumentPageResult result = await _service.GetDocumentPageAsync(
            new ParticipationCertificateDocumentPageRequest
            {
                CongressId = congressId,
                CertificateCulture = NormalizeEmpty(certificateCulture),
                EmailStatus = NormalizeEmpty(emailStatus),
                IncludeRevoked = includeRevoked,
                SearchText = NormalizeEmpty(table.Search?.Value),
                Start = Math.Max(0, table.Start),
                Length = Math.Clamp(table.Length <= 0 ? 25 : table.Length, 10, 250),
                SortColumn = ResolveSortColumn(table),
                SortDirection = table.Order.FirstOrDefault()?.Dir
            },
            cancellationToken);

        return Json(new
        {
            draw = table.Draw,
            recordsTotal = result.TotalCount,
            recordsFiltered = result.FilteredCount,
            data = result.Items
        });
    }

    [HttpPost("generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        [FromBody] ParticipationCertificateGenerateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ParticipationCertificateGenerationJobDto job = await _service.QueueGenerationAsync(
                new ParticipationCertificateGenerationQueueInput
                {
                    CongressId = request.CongressId,
                    CertificateCulture = request.CertificateCulture,
                    SubmissionStatusCode = NormalizeEmpty(request.SubmissionStatusCode),
                    PaymentStatusCode = NormalizeEmpty(request.PaymentStatusCode),
                    CandidateSearch = NormalizeEmpty(request.CandidateSearch),
                    SelectAllFiltered = request.SelectAllFiltered,
                    SelectedCandidateKeys = NormalizeCandidateKeys(request.SelectedCandidateKeys),
                    ExcludedCandidateKeys = NormalizeCandidateKeys(request.ExcludedCandidateKeys),
                    RequestedByUserId = GetCurrentUserId()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = $"{ParticipationCertificateCultures.GetDisplayName(job.Culture)} belge üretim işi kuyruğa alındı.",
                job
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
        catch (Exception exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            _logger.LogError(
                exception,
                "Participation certificate generation queue failed. CongressId: {CongressId}, TraceId: {TraceId}",
                request.CongressId,
                traceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = $"Belge üretim işi başlatılamadı. Hata kodu: {traceId}" });
        }
    }

    [HttpGet("generation/{jobId:guid}/status")]
    public async Task<IActionResult> GenerationStatus(Guid jobId, CancellationToken cancellationToken)
    {
        ParticipationCertificateGenerationJobDto? job = await _service.GetGenerationJobAsync(jobId, cancellationToken);
        return job is null ? NotFound() : Json(job);
    }

    [HttpPost("generation/{jobId:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelGeneration(
        Guid jobId,
        [FromForm] Guid congressId,
        [FromForm] string? certificateCulture,
        CancellationToken cancellationToken)
    {
        await _service.CancelGenerationJobAsync(jobId, GetCurrentUserId(), cancellationToken);
        TempData["SuccessMessage"] = "Belge üretim işi için iptal talebi alındı.";
        return RedirectToAction(nameof(Index), new
        {
            culture = GetRouteCulture(),
            congressId,
            certificateCulture = ParticipationCertificateCultures.Normalize(certificateCulture)
        });
    }

    [HttpPost("queue-emails")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QueueEmails(
        [FromBody] ParticipationCertificateEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ParticipationCertificateOperationResult result = await _service.RequestEmailQueueAsync(
                new ParticipationCertificateEmailQueueInput
                {
                    CongressId = request.CongressId,
                    CertificateCulture = NormalizeEmpty(request.CertificateCulture),
                    EmailStatus = NormalizeEmpty(request.EmailStatus),
                    CandidateSearch = NormalizeEmpty(request.SearchText),
                    SelectAllFiltered = request.SelectAllFiltered,
                    CertificateIds = NormalizeGuidIds(request.CertificateIds),
                    ExcludedCertificateIds = NormalizeGuidIds(request.ExcludedCertificateIds),
                    RequestedByUserId = GetCurrentUserId()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = $"{result.EmailQueuedCount} belge için public link içeren mail arka plan kuyruğuna alındı.",
                result
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
        catch (Exception exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            _logger.LogError(
                exception,
                "Participation certificate email queue failed. CongressId: {CongressId}, TraceId: {TraceId}",
                request.CongressId,
                traceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = $"Mail kuyruğu başlatılamadı. Hata kodu: {traceId}" });
        }
    }

    [HttpPost("revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(
        [FromBody] ParticipationCertificateRevokeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ParticipationCertificateRevokeResult result = await _service.RevokeAsync(
                request.CertificateId,
                request.Reason,
                GetCurrentUserId(),
                cancellationToken);

            return Json(new
            {
                success = true,
                message = result.AlreadyRevoked
                    ? "Belge daha önce kaldırılmış."
                    : result.StorageDeleteSucceeded
                        ? "Belge kaldırıldı; public link iptal edildi, Dokümanlar bölümünden gizlendi ve depolama dosyası silindi."
                        : "Belge ve public link kaldırıldı. Depolama dosyası geçici bir hata nedeniyle silinemedi; hata loglara yazıldı."
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
        catch (Exception exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            _logger.LogError(
                exception,
                "Participation certificate revoke failed. CertificateId: {CertificateId}, TraceId: {TraceId}",
                request.CertificateId,
                traceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = $"Belge kaldırılamadı. Hata kodu: {traceId}" });
        }
    }

    [HttpGet("download/{certificateId:guid}")]
    public async Task<IActionResult> Download(Guid certificateId, CancellationToken cancellationToken)
    {
        ParticipationCertificateStoredFileDto? file = await _service.GetGeneratedFileAsync(
            certificateId,
            cancellationToken);

        if (file is null || string.IsNullOrWhiteSpace(file.BucketName) || string.IsNullOrWhiteSpace(file.ObjectName))
            return NotFound("Katılım belgesi bulunamadı.");

        try
        {
            Stream stream = await _objectStorageService.OpenReadAsync(
                file.BucketName,
                file.ObjectName,
                cancellationToken);
            return File(stream, ResolveContentType(file.ContentType), BuildCertificateDownloadName(file));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Participation certificate download failed. CertificateId: {CertificateId}", certificateId);
            return Problem("Katılım belgesi dosyası depolama alanından okunamadı.");
        }
    }

    [HttpPost("download-selected")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadSelected(
        [FromForm] Guid congressId,
        [FromForm] List<Guid>? certificateIds,
        CancellationToken cancellationToken)
    {
        List<Guid> selectedIds = NormalizeGuidIds(certificateIds ?? new List<Guid>())
            .Take(MaxZipFileCount)
            .ToList();

        if (selectedIds.Count == 0)
            return BadRequest("Lütfen en az bir katılım belgesi seçin.");

        IReadOnlyList<ParticipationCertificateStoredFileDto> files = await _service.GetGeneratedFilesAsync(
            congressId,
            selectedIds,
            cancellationToken);
        return await CreateZipDownloadAsync(congressId, files, cancellationToken);
    }

    [HttpPost("download-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadAll(
        [FromForm] Guid congressId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ParticipationCertificateStoredFileDto> files = await _service.GetGeneratedFilesAsync(
            congressId,
            cancellationToken: cancellationToken);

        if (files.Count > MaxZipFileCount)
        {
            return BadRequest(
                $"Tek seferde en fazla {MaxZipFileCount} katılım belgesi indirilebilir. Lütfen seçim yaparak indirin.");
        }

        return await CreateZipDownloadAsync(congressId, files, cancellationToken);
    }

    private async Task<IActionResult> CreateZipDownloadAsync(
        Guid congressId,
        IReadOnlyList<ParticipationCertificateStoredFileDto> files,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty || files.Count == 0)
            return NotFound("İndirilecek katılım belgesi bulunamadı.");

        string tempPath = Path.Combine(Path.GetTempPath(), $"symplify-participation-certificates-{Guid.NewGuid():N}.zip");
        int addedCount = 0;
        HashSet<string> usedEntryNames = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            await using (FileStream zipFileStream = new(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.ReadWrite,
                             FileShare.None,
                             1024 * 64,
                             FileOptions.Asynchronous))
            {
                using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Create, leaveOpen: true);

                foreach (ParticipationCertificateStoredFileDto file in files)
                {
                    if (string.IsNullOrWhiteSpace(file.BucketName) || string.IsNullOrWhiteSpace(file.ObjectName))
                        continue;

                    ZipArchiveEntry? entry = null;
                    try
                    {
                        await using Stream objectStream = await _objectStorageService.OpenReadAsync(
                            file.BucketName,
                            file.ObjectName,
                            cancellationToken);
                        string entryName = BuildUniqueZipEntryName(file, usedEntryNames);
                        entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                        await using Stream entryStream = entry.Open();
                        await objectStream.CopyToAsync(entryStream, cancellationToken);
                        addedCount++;
                    }
                    catch (Exception exception)
                    {
                        entry?.Delete();
                        _logger.LogWarning(
                            exception,
                            "Participation certificate skipped while creating ZIP. CertificateId: {CertificateId}",
                            file.Id);
                    }
                }
            }

            if (addedCount == 0)
            {
                System.IO.File.Delete(tempPath);
                return NotFound("Katılım belgesi dosyaları depolama alanından okunamadı.");
            }

            FileStream downloadStream = new(
                tempPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 64,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);

            return File(downloadStream, "application/zip", $"katilim-belgeleri-{DateTime.UtcNow:yyyyMMdd-HHmm}.zip");
        }
        catch
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
            throw;
        }
    }

    private ParticipationCertificatesIndexViewModel CreateEmptyModel(
        IReadOnlyList<ParticipationCertificateCongressOptionDto> congresses)
    {
        return new ParticipationCertificatesIndexViewModel
        {
            CongressId = Guid.Empty,
            CongressTitle = congresses.Count == 0 ? "Kongre bulunamadı" : "Kongre seçiniz",
            CongressOptions = BuildCongressOptions(congresses, null),
            CertificateCultureOptions = BuildCertificateCultureOptions(ParticipationCertificateCultures.Turkish),
            SubmissionStatusOptions = BuildFilterOptions(Array.Empty<ParticipationCertificateFilterOptionDto>(), null, "Tüm bildiri durumları"),
            PaymentStatusOptions = BuildFilterOptions(Array.Empty<ParticipationCertificateFilterOptionDto>(), null, "Tüm ödeme durumları")
        };
    }

    private static string? ResolveSortColumn(DataTableRequest table)
    {
        DataTableOrderRequest? order = table.Order.FirstOrDefault();
        if (order is null || order.Column < 0 || order.Column >= table.Columns.Count)
            return null;
        return NormalizeEmpty(table.Columns[order.Column].Name) ?? NormalizeEmpty(table.Columns[order.Column].Data);
    }

    private static IReadOnlyList<string> NormalizeCandidateKeys(IEnumerable<string>? values)
    {
        return (values ?? Array.Empty<string>())
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(IsValidCandidateKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSelectionCount)
            .ToList();
    }

    private static bool IsValidCandidateKey(string value)
    {
        if (Guid.TryParseExact(value, "N", out _))
            return true;

        // Eski kişi-bazlı isteklerle geriye dönük uyumluluk.
        string[] parts = value.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 &&
               Guid.TryParseExact(parts[0], "N", out _) &&
               Guid.TryParseExact(parts[1], "N", out _);
    }

    private static List<Guid> NormalizeGuidIds(IEnumerable<Guid>? values)
        => (values ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(MaxSelectionCount)
            .ToList();

    private static string BuildUniqueZipEntryName(
        ParticipationCertificateStoredFileDto file,
        ISet<string> usedEntryNames)
    {
        string submission = SanitizeFileNamePart(file.SubmissionNumber, "bildiri");
        string author = SanitizeFileNamePart(file.AuthorFullName, "yazar");
        string languageCode = ParticipationCertificateCultures.GetShortCode(file.Culture);
        string baseName = $"{submission}-{author}-{languageCode}";
        string candidate = $"{baseName}.pdf";

        if (usedEntryNames.Add(candidate))
            return candidate;

        candidate = $"{baseName}-{file.Id.ToString("N")[..8]}.pdf";
        usedEntryNames.Add(candidate);
        return candidate;
    }

    private static string BuildCertificateDownloadName(ParticipationCertificateStoredFileDto file)
    {
        string submission = SanitizeFileNamePart(file.SubmissionNumber, "bildiri");
        string author = SanitizeFileNamePart(file.AuthorFullName, "yazar");
        string languageCode = ParticipationCertificateCultures.GetShortCode(file.Culture);

        return string.Equals(
                ParticipationCertificateCultures.Normalize(file.Culture),
                ParticipationCertificateCultures.English,
                StringComparison.OrdinalIgnoreCase)
            ? $"certificate-of-participation-{submission}-{author}-{languageCode}.pdf"
            : $"katilim-belgesi-{submission}-{author}-{languageCode}.pdf";
    }

    private static string SanitizeFileNamePart(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        HashSet<char> invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        string sanitized = new(value
            .Trim()
            .Select(character => invalidChars.Contains(character) || character is '/' or '\\' or ':' ? '-' : character)
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);

        sanitized = sanitized.Trim(' ', '.', '-', '_');
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }

    private static string ResolveContentType(string? contentType)
        => string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType.Trim();

    private string GetRouteCulture()
        => RouteData.Values["culture"]?.ToString() ?? "tr-TR";

    private Guid? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out Guid id) ? id : null;
    }

    private static IReadOnlyList<SelectListItem> BuildCongressOptions(
        IReadOnlyList<ParticipationCertificateCongressOptionDto> congresses,
        Guid? selectedCongressId)
    {
        return congresses.Select(item => new SelectListItem
        {
            Value = item.Id.ToString("D"),
            Text = item.Text,
            Selected = selectedCongressId.HasValue && item.Id == selectedCongressId.Value
        }).ToList();
    }

    private static IReadOnlyList<SelectListItem> BuildCertificateCultureOptions(string? selectedCulture)
    {
        string normalizedSelected = ParticipationCertificateCultures.Normalize(selectedCulture);
        return ParticipationCertificateCultures.Supported.Select(culture => new SelectListItem
        {
            Value = culture,
            Text = ParticipationCertificateCultures.GetDisplayName(culture),
            Selected = string.Equals(culture, normalizedSelected, StringComparison.OrdinalIgnoreCase)
        }).ToList();
    }

    private static IReadOnlyList<SelectListItem> BuildFilterOptions(
        IReadOnlyList<ParticipationCertificateFilterOptionDto> options,
        string? selectedCode,
        string allText)
    {
        List<SelectListItem> items = new()
        {
            new SelectListItem
            {
                Value = string.Empty,
                Text = allText,
                Selected = string.IsNullOrWhiteSpace(selectedCode)
            }
        };

        items.AddRange(options.Select(option => new SelectListItem
        {
            Value = option.Code,
            Text = option.Count > 0 ? $"{option.Text} ({option.Count})" : option.Text,
            Selected = string.Equals(option.Code, selectedCode, StringComparison.OrdinalIgnoreCase)
        }));
        return items;
    }

    private static string? NormalizeEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
