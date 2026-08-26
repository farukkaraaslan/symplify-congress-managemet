using System.Globalization;
using System.Net;
using Core.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Storage;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.ShortLinks;
using Symplify.BackOffice.Domain.Submission;
using CongressEntity = Symplify.BackOffice.Domain.Congress.Congress;

namespace Symplify.BackOffice.WebUI.Controllers;

[AllowAnonymous]
public sealed class ShortLinksController : Controller
{
    private readonly IShortLinkRepository _shortLinkRepository;
    private readonly ISubmissionFileRepository _submissionFileRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IObjectStorageRangeReader _objectStorageRangeReader;
    private readonly ObjectStorageOptions _objectStorageOptions;

    public ShortLinksController(
        IShortLinkRepository shortLinkRepository,
        ISubmissionFileRepository submissionFileRepository,
        IObjectStorageService objectStorageService,
        IObjectStorageRangeReader objectStorageRangeReader,
        IOptions<ObjectStorageOptions> objectStorageOptions)
    {
        _shortLinkRepository = shortLinkRepository;
        _submissionFileRepository = submissionFileRepository;
        _objectStorageService = objectStorageService;
        _objectStorageRangeReader = objectStorageRangeReader;
        _objectStorageOptions = objectStorageOptions.Value;
    }

    [HttpGet("v/{code}")]
    public Task<IActionResult> ResolveVideo(string code, CancellationToken cancellationToken)
        => ShowVideoPlayerAsync(code, cancellationToken);

    [HttpHead("v/{code}")]
    [HttpGet("v/{code}/stream")]
    [HttpHead("v/{code}/stream")]
    public Task<IActionResult> StreamVideo(string code, CancellationToken cancellationToken)
        => ResolveStreamAsync(code, ShortLinkTargetType.SubmissionPresentationVideo, cancellationToken);

    [HttpGet("f/{code}")]
    [HttpHead("f/{code}")]
    public Task<IActionResult> ResolveFullText(string code, CancellationToken cancellationToken)
        => ResolveStreamAsync(code, ShortLinkTargetType.SubmissionFullText, cancellationToken);

    private async Task<IActionResult> ShowVideoPlayerAsync(
        string code,
        CancellationToken cancellationToken)
    {
        CultureInfo pageCulture = ResolveRequestedCultureInstance();
        CultureInfo.CurrentCulture = pageCulture;
        CultureInfo.CurrentUICulture = pageCulture;

        ResolvedShortLinkFile? resolved = await ResolveShortLinkFileAsync(
            code,
            ShortLinkTargetType.SubmissionPresentationVideo,
            cancellationToken);

        if (resolved is null)
            return NotFound();

        string normalizedCode = NormalizeCode(code);
        string encodedCode = Uri.EscapeDataString(normalizedCode);
        string streamUrl = Url.Content($"~/v/{encodedCode}/stream");
        string contentType = ResolveContentType(resolved.File, resolved.FileInfo.ContentType);

        Submission submission = resolved.File.Submission;
        VideoPageLabels labels = ResolveVideoPageLabels(pageCulture);
        string congressName = ResolveCongressName(submission.Congress, pageCulture);
        string title = ResolveSubmissionTitle(submission, pageCulture);
        string submissionNumber = ResolveSubmissionNumber(submission);
        string submissionTypeName = ResolveSubmissionTypeName(submission, pageCulture);
        string authorsText = ResolveAuthorsText(submission, pageCulture);
        string fileName = string.IsNullOrWhiteSpace(resolved.File.OriginalFileName)
            ? labels.VideoPresentation
            : Path.GetFileName(resolved.File.OriginalFileName);

        IReadOnlyList<RelatedVideoPresentation> relatedVideos = await LoadRelatedVideoPresentationsAsync(
            submission,
            resolved.File.Id,
            pageCulture,
            cancellationToken);

        string relatedVideosHtml = RenderRelatedVideosHtml(
            relatedVideos,
            labels,
            pageCulture);

        string encodedTitle = WebUtility.HtmlEncode(title);
        string encodedCongressName = WebUtility.HtmlEncode(congressName);
        string encodedSubmissionNumber = WebUtility.HtmlEncode(submissionNumber);
        string encodedSubmissionTypeName = WebUtility.HtmlEncode(submissionTypeName);
        string encodedAuthorsText = WebUtility.HtmlEncode(authorsText);
        string encodedFileName = WebUtility.HtmlEncode(fileName);
        string encodedStreamUrl = WebUtility.HtmlEncode(streamUrl);
        string encodedContentType = WebUtility.HtmlEncode(contentType);
        string encodedSeparateTabText = WebUtility.HtmlEncode(labels.SeparateTabText);
        string encodedBrowserNotSupported = WebUtility.HtmlEncode(labels.BrowserNotSupported);
        string encodedSubmissionDetailsLabel = WebUtility.HtmlEncode(labels.SubmissionDetails);
        string encodedAuthorsLabel = WebUtility.HtmlEncode(labels.Authors);
        string encodedSubmissionLabel = WebUtility.HtmlEncode(labels.SubmissionNumber);
        string encodedTypeLabel = WebUtility.HtmlEncode(labels.SubmissionType);
        string encodedFileLabel = WebUtility.HtmlEncode(labels.File);
        string encodedLanguageLabel = WebUtility.HtmlEncode(labels.Language);
        string encodedTurkishLabel = WebUtility.HtmlEncode(labels.Turkish);
        string encodedEnglishLabel = WebUtility.HtmlEncode(labels.English);
        string trUrl = WebUtility.HtmlEncode(BuildCultureUrl(normalizedCode, "tr-TR"));
        string enUrl = WebUtility.HtmlEncode(BuildCultureUrl(normalizedCode, "en-US"));
        string trActiveClass = pageCulture.Name.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ? " is-active" : string.Empty;
        string enActiveClass = pageCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? " is-active" : string.Empty;
        string htmlLang = pageCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "tr";

        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        string html = $$"""
<!doctype html>
<html lang="{{htmlLang}}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{encodedTitle}}</title>
    <style>
        :root {
            color-scheme: dark;
            --page-bg: #0f172a;
            --panel-bg: #111827;
            --panel-soft: #1f2937;
            --border: rgba(148, 163, 184, .22);
            --text: #f8fafc;
            --muted: #94a3b8;
            --brand: #3b82f6;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            background: radial-gradient(circle at top left, rgba(59,130,246,.22), transparent 32%), var(--page-bg);
            color: var(--text);
        }
        .page {
            min-height: 100vh;
            padding: 28px;
        }
        .shell {
            width: min(1320px, 100%);
            margin: 0 auto;
        }
        .topbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
            margin-bottom: 18px;
        }
        .brand {
            display: inline-flex;
            align-items: center;
            gap: 10px;
            color: #bfdbfe;
            font-weight: 800;
            letter-spacing: .01em;
            min-width: 0;
        }
        .brand-text {
            display: block;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }
        .brand-mark {
            width: 34px;
            height: 34px;
            flex: 0 0 34px;
            border-radius: 999px;
            background: linear-gradient(135deg, #2563eb, #38bdf8);
            display: grid;
            place-items: center;
            box-shadow: 0 12px 30px rgba(37,99,235,.32);
        }
        .language-switcher {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            color: var(--muted);
            font-size: 13px;
            white-space: nowrap;
        }
        .language-link {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            min-width: 42px;
            padding: 7px 10px;
            border: 1px solid var(--border);
            border-radius: 999px;
            color: #cbd5e1;
            background: rgba(15, 23, 42, .62);
            text-decoration: none;
            font-weight: 700;
        }
        .language-link:hover,
        .language-link.is-active {
            color: #eff6ff;
            border-color: rgba(96, 165, 250, .65);
            background: rgba(37, 99, 235, .3);
        }
        .layout {
            display: grid;
            grid-template-columns: minmax(0, 1fr) 380px;
            gap: 22px;
            align-items: start;
        }
        .video-card {
            background: rgba(17, 24, 39, .92);
            border: 1px solid var(--border);
            border-radius: 22px;
            overflow: hidden;
            box-shadow: 0 24px 80px rgba(0,0,0,.34);
        }
        .video-frame {
            background: #000;
            aspect-ratio: 16 / 9;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        video {
            display: block;
            width: 100%;
            height: 100%;
            background: #000;
        }
        .video-info {
            padding: 22px 24px 24px;
        }
        h1 {
            margin: 0 0 14px;
            font-size: clamp(22px, 2.6vw, 34px);
            line-height: 1.22;
            font-weight: 850;
            letter-spacing: -.025em;
        }
        .meta-row {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            color: var(--muted);
            font-size: 14px;
        }
        .pill {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 8px 11px;
            border: 1px solid var(--border);
            border-radius: 999px;
            background: rgba(15, 23, 42, .76);
        }
        .side-stack {
            display: grid;
            gap: 16px;
        }
        .side-card {
            background: rgba(17, 24, 39, .86);
            border: 1px solid var(--border);
            border-radius: 22px;
            padding: 22px;
            box-shadow: 0 18px 55px rgba(0,0,0,.24);
        }
        .side-title {
            margin: 0 0 16px;
            font-size: 16px;
            font-weight: 800;
            color: #dbeafe;
        }
        .detail-list {
            display: grid;
            gap: 16px;
        }
        .detail-item {
            padding-bottom: 16px;
            border-bottom: 1px solid var(--border);
        }
        .detail-item:last-child {
            padding-bottom: 0;
            border-bottom: 0;
        }
        .detail-label {
            margin-bottom: 6px;
            color: var(--muted);
            font-size: 12px;
            text-transform: uppercase;
            letter-spacing: .08em;
        }
        .detail-value {
            color: #e5e7eb;
            font-weight: 600;
            line-height: 1.45;
            white-space: pre-line;
            word-break: break-word;
        }
        .related-toolbar {
            display: grid;
            gap: 10px;
            margin-bottom: 12px;
        }
        .related-count {
            color: var(--muted);
            font-size: 12px;
            line-height: 1.35;
        }
        .related-search {
            width: 100%;
            min-height: 40px;
            padding: 9px 12px;
            border: 1px solid var(--border);
            border-radius: 12px;
            outline: none;
            color: var(--text);
            background: rgba(15, 23, 42, .72);
            font: inherit;
            font-size: 13px;
        }
        .related-search::placeholder { color: #64748b; }
        .related-search:focus {
            border-color: rgba(96, 165, 250, .7);
            box-shadow: 0 0 0 3px rgba(59, 130, 246, .12);
        }
        .related-list {
            display: grid;
            gap: 10px;
            max-height: min(70vh, 760px);
            overflow-y: auto;
            overscroll-behavior: contain;
            padding-right: 5px;
            scrollbar-width: thin;
            scrollbar-color: rgba(148, 163, 184, .42) transparent;
        }
        .related-list::-webkit-scrollbar { width: 7px; }
        .related-list::-webkit-scrollbar-track { background: transparent; }
        .related-list::-webkit-scrollbar-thumb {
            border-radius: 999px;
            background: rgba(148, 163, 184, .36);
        }
        .related-video {
            display: block;
            padding: 12px;
            border: 1px solid var(--border);
            border-radius: 14px;
            color: inherit;
            background: rgba(15, 23, 42, .55);
            text-decoration: none;
            transition: border-color .16s ease, background .16s ease, transform .16s ease;
        }
        .related-video:hover {
            border-color: rgba(96, 165, 250, .55);
            background: rgba(30, 41, 59, .78);
            transform: translateY(-1px);
        }
        .related-title {
            display: block;
            color: #f8fafc;
            font-size: 13px;
            font-weight: 800;
            line-height: 1.32;
        }
        .related-video[hidden] { display: none; }
        .related-meta,
        .related-authors,
        .related-empty {
            display: block;
            margin-top: 6px;
            color: var(--muted);
            font-size: 12px;
            line-height: 1.38;
        }
        .help {
            margin-top: 16px;
            color: var(--muted);
            font-size: 13px;
        }
        .help a {
            color: #93c5fd;
            text-decoration: none;
            font-weight: 600;
        }
        .help a:hover { text-decoration: underline; }
        @media (max-width: 980px) {
            .page { padding: 16px; }
            .topbar { align-items: flex-start; flex-direction: column; }
            .layout { grid-template-columns: 1fr; }
            .side-stack { order: 2; }
        }
    </style>
</head>
<body>
    <main class="page">
        <div class="shell">
            <div class="topbar">
                <div class="brand"><span class="brand-mark">▶</span><span class="brand-text">{{encodedCongressName}}</span></div>
                <div class="language-switcher" aria-label="{{encodedLanguageLabel}}">
                    <span>{{encodedLanguageLabel}}</span>
                    <a class="language-link{{trActiveClass}}" href="{{trUrl}}">TR</a>
                    <a class="language-link{{enActiveClass}}" href="{{enUrl}}">EN</a>
                </div>
            </div>

            <div class="layout">
                <section class="video-card">
                    <div class="video-frame">
                        <video controls preload="metadata" playsinline controlsList="nodownload">
                            <source src="{{encodedStreamUrl}}" type="{{encodedContentType}}">
                            {{encodedBrowserNotSupported}}
                        </video>
                    </div>
                    <div class="video-info">
                        <h1>{{encodedTitle}}</h1>
                        <div class="meta-row">
                            <span class="pill">{{encodedSubmissionNumber}}</span>
                            <span class="pill">{{encodedSubmissionTypeName}}</span>
                        </div>
                        <div class="help">
                            <a href="{{encodedStreamUrl}}" target="_blank" rel="noopener">{{encodedSeparateTabText}}</a>
                        </div>
                    </div>
                </section>

                <aside class="side-stack">
                    <section class="side-card">
                        <h2 class="side-title">{{encodedSubmissionDetailsLabel}}</h2>
                        <div class="detail-list">
                            <div class="detail-item">
                                <div class="detail-label">{{encodedAuthorsLabel}}</div>
                                <div class="detail-value">{{encodedAuthorsText}}</div>
                            </div>
                            <div class="detail-item">
                                <div class="detail-label">{{encodedSubmissionLabel}}</div>
                                <div class="detail-value">{{encodedSubmissionNumber}}</div>
                            </div>
                            <div class="detail-item">
                                <div class="detail-label">{{encodedTypeLabel}}</div>
                                <div class="detail-value">{{encodedSubmissionTypeName}}</div>
                            </div>
                            <div class="detail-item">
                                <div class="detail-label">{{encodedFileLabel}}</div>
                                <div class="detail-value">{{encodedFileName}}</div>
                            </div>
                        </div>
                    </section>

                    {{relatedVideosHtml}}
                </aside>
            </div>
        </div>
    </main>
    <script>
        (() => {
            const search = document.getElementById('relatedVideoSearch');
            const list = document.getElementById('relatedVideoList');
            const visibleCount = document.getElementById('relatedVideoVisibleCount');
            if (!search || !list) return;

            const items = Array.from(list.querySelectorAll('.related-video'));
            const locale = document.documentElement.lang || 'tr';

            const filter = () => {
                const term = search.value.trim().toLocaleLowerCase(locale);
                let shown = 0;

                items.forEach(item => {
                    const haystack = (item.dataset.search || item.textContent || '')
                        .toLocaleLowerCase(locale);
                    const visible = !term || haystack.includes(term);
                    item.hidden = !visible;
                    if (visible) shown++;
                });

                if (visibleCount) visibleCount.textContent = String(shown);
            };

            search.addEventListener('input', filter, { passive: true });
        })();
    </script>
</body>
</html>
""";

        return Content(html, "text/html; charset=utf-8");
    }

    private async Task<IActionResult> ResolveStreamAsync(
        string code,
        ShortLinkTargetType expectedTargetType,
        CancellationToken cancellationToken)
    {
        ResolvedShortLinkFile? resolved = await ResolveShortLinkFileAsync(
            code,
            expectedTargetType,
            cancellationToken);

        if (resolved is null)
            return NotFound();

        if (!TryResolveByteRange(Request.Headers["Range"].ToString(), resolved.FileInfo.Size, out ByteRange range))
            return RangeNotSatisfiable(resolved.FileInfo.Size);

        if (!HttpMethods.IsHead(Request.Method) && (!range.IsPartial || range.Start == 0))
            await MarkLinkAccessedAsync(resolved.ShortLink, cancellationToken);

        ConfigureFileResponse(resolved.File, resolved.FileInfo, range);

        if (HttpMethods.IsHead(Request.Method))
            return new EmptyResult();

        try
        {
            await _objectStorageRangeReader.CopyRangeToAsync(
                resolved.BucketName,
                resolved.ObjectName,
                Response.Body,
                range.Start,
                range.Length,
                HttpContext.RequestAborted);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // The client closed the tab, stopped the download or moved the video seek position.
        }

        return new EmptyResult();
    }

    private async Task<ResolvedShortLinkFile?> ResolveShortLinkFileAsync(
        string code,
        ShortLinkTargetType expectedTargetType,
        CancellationToken cancellationToken)
    {
        string normalizedCode = NormalizeCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return null;

        ShortLink? shortLink = await _shortLinkRepository
            .Query()
            .FirstOrDefaultAsync(item =>
                item.Code == normalizedCode &&
                item.TargetType == expectedTargetType &&
                item.DeletedDate == null &&
                item.IsActive,
                cancellationToken);

        if (shortLink is null || IsExpired(shortLink))
            return null;

        SubmissionFile? file = await _submissionFileRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Congress)
                    .ThenInclude(congress => congress.Translations)
                        .ThenInclude(translation => translation.Language)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.TransactionStatus)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.PaymentStatus)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Authors)
                    .ThenInclude(author => author.Title)
                        .ThenInclude(title => title!.Translations)
                            .ThenInclude(translation => translation.Language)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.SubmissionType)
                    .ThenInclude(type => type!.Translations)
                        .ThenInclude(translation => translation.Language)
            .FirstOrDefaultAsync(item =>
                item.Id == shortLink.TargetId &&
                item.DeletedDate == null &&
                item.IsActive,
                cancellationToken);

        if (file is null || !CanPubliclyAccess(file, expectedTargetType))
            return null;

        string? bucketName = ResolveSubmissionBucketName();
        if (string.IsNullOrWhiteSpace(bucketName))
            return null;

        string? objectName = ResolveObjectName(file.FilePath, bucketName);
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        ObjectStorageFileInfo? fileInfo = await _objectStorageService.GetFileInfoAsync(
            bucketName,
            objectName,
            cancellationToken);

        if (fileInfo is null || fileInfo.Size <= 0)
            return null;

        return new ResolvedShortLinkFile(shortLink, file, bucketName, objectName, fileInfo);
    }

    private async Task<IReadOnlyList<RelatedVideoPresentation>> LoadRelatedVideoPresentationsAsync(
        Submission currentSubmission,
        Guid currentFileId,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        List<SubmissionFile> files = await _submissionFileRepository
            .Query()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.TransactionStatus)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.PaymentStatus)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Authors)
                    .ThenInclude(author => author.Title)
                        .ThenInclude(title => title!.Translations)
                            .ThenInclude(translation => translation.Language)
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.SubmissionType)
                    .ThenInclude(type => type!.Translations)
                        .ThenInclude(translation => translation.Language)
            .Where(item =>
                item.Id != currentFileId &&
                item.SubmissionId != currentSubmission.Id &&
                item.DeletedDate == null &&
                item.IsActive &&
                item.FileKind == SubmissionFileKind.Presentation &&
                item.ReviewStatus == SubmissionFileReviewStatus.Approved &&
                item.IsIncludedInProgramBook &&
                item.Submission.CongressId == currentSubmission.CongressId)
            .OrderBy(item => item.Submission.SubmissionNumber)
            .ThenByDescending(item => item.VersionNo)
            .ThenBy(item => item.OriginalFileName)
            .ToListAsync(cancellationToken);

        List<SubmissionFile> publicFiles = files
            .Where(file => CanPubliclyAccess(file, ShortLinkTargetType.SubmissionPresentationVideo))
            .ToList();

        if (publicFiles.Count == 0)
            return Array.Empty<RelatedVideoPresentation>();

        List<Guid> fileIds = publicFiles.Select(file => file.Id).ToList();
        List<ShortLink> links = await _shortLinkRepository
            .Query()
            .AsNoTracking()
            .Where(item =>
                fileIds.Contains(item.TargetId) &&
                item.TargetType == ShortLinkTargetType.SubmissionPresentationVideo &&
                item.DeletedDate == null &&
                item.IsActive)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, ShortLink> linkLookup = links
            .Where(link => !IsExpired(link))
            .GroupBy(link => link.TargetId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(link => link.LastAccessedAt ?? DateTime.MinValue).First());

        return publicFiles
            .Where(file => linkLookup.ContainsKey(file.Id))
            .GroupBy(file => file.SubmissionId)
            .Select(group => group
                .OrderByDescending(file => file.VersionNo)
                .ThenByDescending(file => file.CreatedDate)
                .First())
            .OrderBy(file => ResolveSubmissionNumber(file.Submission), StringComparer.OrdinalIgnoreCase)
            .Select(file =>
            {
                ShortLink link = linkLookup[file.Id];
                Submission submission = file.Submission;
                return new RelatedVideoPresentation(
                    link.Code,
                    ResolveSubmissionTitle(submission, culture),
                    ResolveSubmissionNumber(submission),
                    ResolveSubmissionTypeName(submission, culture),
                    ResolveAuthorsInlineText(submission, culture));
            })
            .ToList();
    }

    private string RenderRelatedVideosHtml(
        IReadOnlyList<RelatedVideoPresentation> relatedVideos,
        VideoPageLabels labels,
        CultureInfo culture)
    {
        string title = WebUtility.HtmlEncode(labels.RelatedVideos);

        if (relatedVideos.Count == 0)
        {
            string empty = WebUtility.HtmlEncode(labels.NoRelatedVideos);
            return $$"""
<section class="side-card">
    <h2 class="side-title">{{title}}</h2>
    <span class="related-empty">{{empty}}</span>
</section>
""";
        }

        string cultureName = Uri.EscapeDataString(culture.Name);
        string searchPlaceholder = WebUtility.HtmlEncode(labels.SearchVideos);
        string videoCountLabel = WebUtility.HtmlEncode(labels.VideoCount);
        string items = string.Join(Environment.NewLine, relatedVideos.Select(video =>
        {
            string url = WebUtility.HtmlEncode(Url.Content($"~/v/{Uri.EscapeDataString(video.Code)}?culture={cultureName}"));
            string videoTitle = WebUtility.HtmlEncode(video.Title);
            string meta = WebUtility.HtmlEncode($"{video.SubmissionNumber} · {video.SubmissionTypeName}");
            string authors = WebUtility.HtmlEncode(video.Authors);
            string searchText = WebUtility.HtmlEncode($"{video.Title} {video.SubmissionNumber} {video.SubmissionTypeName} {video.Authors}");

            return $$"""
<a class="related-video" href="{{url}}" data-search="{{searchText}}">
    <span class="related-title">{{videoTitle}}</span>
    <span class="related-meta">{{meta}}</span>
    <span class="related-authors">{{authors}}</span>
</a>
""";
        }));

        return $$"""
<section class="side-card">
    <h2 class="side-title">{{title}}</h2>
    <div class="related-toolbar">
        <span class="related-count"><span id="relatedVideoVisibleCount">{{relatedVideos.Count}}</span> / {{relatedVideos.Count}} {{videoCountLabel}}</span>
        <input id="relatedVideoSearch" class="related-search" type="search" placeholder="{{searchPlaceholder}}" autocomplete="off">
    </div>
    <div class="related-list" id="relatedVideoList">
        {{items}}
    </div>
</section>
""";
    }

    private void ConfigureFileResponse(
        SubmissionFile file,
        ObjectStorageFileInfo fileInfo,
        ByteRange range)
    {
        Response.StatusCode = range.IsPartial
            ? StatusCodes.Status206PartialContent
            : StatusCodes.Status200OK;

        Response.ContentType = ResolveContentType(file, fileInfo.ContentType);
        Response.ContentLength = range.Length;
        Response.Headers["Accept-Ranges"] = "bytes";
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["Content-Disposition"] = BuildInlineContentDisposition(file.OriginalFileName, file.FilePath);

        if (range.IsPartial)
            Response.Headers["Content-Range"] = $"bytes {range.Start}-{range.End}/{range.TotalLength}";
    }

    private IActionResult RangeNotSatisfiable(long totalLength)
    {
        Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
        Response.Headers["Accept-Ranges"] = "bytes";
        Response.Headers["Content-Range"] = $"bytes */{totalLength}";
        Response.Headers["Cache-Control"] = "no-store";
        return new EmptyResult();
    }

    private async Task MarkLinkAccessedAsync(ShortLink shortLink, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        shortLink.ClickCount++;
        shortLink.LastAccessedAt = DateTime.UtcNow;
        shortLink.UpdatedDate = DateTime.UtcNow;
        shortLink.UpdatedBy = "ShortLinkResolver";

        await _shortLinkRepository.UpdateAsync(shortLink);
    }

    private static bool CanPubliclyAccess(
        SubmissionFile file,
        ShortLinkTargetType targetType)
    {
        bool acceptedAndPaid =
            IsAccepted(file.Submission.TransactionStatus?.Code, file.Submission.TransactionStatusId) &&
            IsPaymentCompleted(file.Submission.PaymentStatus?.Code, file.Submission.PaymentStatusId);

        if (!acceptedAndPaid || file.ReviewStatus != SubmissionFileReviewStatus.Approved)
            return false;

        return targetType switch
        {
            ShortLinkTargetType.SubmissionPresentationVideo =>
                file.FileKind == SubmissionFileKind.Presentation &&
                file.IsIncludedInProgramBook,

            ShortLinkTargetType.SubmissionFullText =>
                file.FileKind == SubmissionFileKind.FullText,

            _ => false
        };
    }

    private static bool IsAccepted(string? statusCode, int? statusId)
    {
        if (statusId == (int)TransactionStatus.Accepted)
            return true;

        return IsCode(statusCode, "ACCEPTED", "ACCEPT", "APPROVED");
    }

    private static bool IsPaymentCompleted(string? paymentStatusCode, int? paymentStatusId)
    {
        if (paymentStatusId == (int)PaymentStatus.Approved)
            return true;

        return IsCode(
            paymentStatusCode,
            "PAID",
            "PAYMENT_PAID",
            "PAYMENT_COMPLETED",
            "APPROVED",
            "PAYMENT_APPROVED",
            "COMPLETED");
    }

    private static bool IsCode(string? actualCode, params string[] expectedCodes)
    {
        if (string.IsNullOrWhiteSpace(actualCode))
            return false;

        string normalized = actualCode
            .Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        return expectedCodes.Any(expected =>
            string.Equals(normalized, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryResolveByteRange(
        string? rangeHeader,
        long totalLength,
        out ByteRange range)
    {
        range = new ByteRange(0, totalLength - 1, totalLength, false);

        if (totalLength <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(rangeHeader))
            return true;

        string normalized = rangeHeader.Trim();
        if (!normalized.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return false;

        string value = normalized["bytes=".Length..].Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Contains(','))
            return false;

        int separatorIndex = value.IndexOf('-');
        if (separatorIndex < 0)
            return false;

        string startText = value[..separatorIndex].Trim();
        string endText = value[(separatorIndex + 1)..].Trim();

        long start;
        long end;

        if (string.IsNullOrWhiteSpace(startText))
        {
            if (!long.TryParse(endText, out long suffixLength) || suffixLength <= 0)
                return false;

            long resolvedLength = Math.Min(suffixLength, totalLength);
            start = totalLength - resolvedLength;
            end = totalLength - 1;
        }
        else
        {
            if (!long.TryParse(startText, out start) || start < 0 || start >= totalLength)
                return false;

            if (string.IsNullOrWhiteSpace(endText))
            {
                end = totalLength - 1;
            }
            else
            {
                if (!long.TryParse(endText, out end) || end < start)
                    return false;

                end = Math.Min(end, totalLength - 1);
            }
        }

        range = new ByteRange(start, end, totalLength, true);
        return true;
    }

    private static string? ResolveObjectName(string? storedPath, string bucketName)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        string candidate = storedPath.Trim();

        if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri? absoluteUri))
        {
            candidate = Uri.UnescapeDataString(absoluteUri.AbsolutePath).TrimStart('/');
        }
        else
        {
            candidate = candidate.Replace('\\', '/').TrimStart('/');
        }

        string bucketPrefix = $"{bucketName.Trim().Trim('/')}/";
        if (candidate.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
            candidate = candidate[bucketPrefix.Length..];

        candidate = candidate.TrimStart('/');

        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        string[] segments = candidate.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".."))
            return null;

        return string.Join('/', segments);
    }

    private CultureInfo ResolveRequestedCultureInstance()
    {
        string? value = null;

        if (Request.Query.TryGetValue("culture", out Microsoft.Extensions.Primitives.StringValues cultureValues))
            value = cultureValues.FirstOrDefault();
        else if (Request.Query.TryGetValue("lang", out Microsoft.Extensions.Primitives.StringValues langValues))
            value = langValues.FirstOrDefault();
        else if (Request.Query.TryGetValue("ui-culture", out Microsoft.Extensions.Primitives.StringValues uiCultureValues))
            value = uiCultureValues.FirstOrDefault();

        string normalized = NormalizeCulture(value);
        return CultureInfo.GetCultureInfo(normalized);
    }

    private static string NormalizeCulture(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            string current = CultureInfo.CurrentUICulture.Name;
            if (current.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "en-US";

            return "tr-TR";
        }

        string normalized = value.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "en" or "en-us" => "en-US",
            "tr" or "tr-tr" => "tr-TR",
            _ => "tr-TR"
        };
    }

    private string BuildCultureUrl(string code, string culture)
    {
        string encodedCode = Uri.EscapeDataString(code);
        string encodedCulture = Uri.EscapeDataString(culture);
        return Url.Content($"~/v/{encodedCode}?culture={encodedCulture}");
    }

    private string ResolveCongressName(CongressEntity? congress, CultureInfo culture)
    {
        if (congress is null)
            return ResolveVideoPageLabels(culture).VideoPresentation;

        string? translated = congress.Translations
            .Where(translation => translation.DeletedDate == null && string.Equals(translation.Language.Culture, culture.Name, StringComparison.OrdinalIgnoreCase))
            .Select(translation => translation.Title)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(translated))
            return translated.Trim();

        string? defaultTranslated = congress.Translations
            .Where(translation => translation.DeletedDate == null)
            .OrderByDescending(translation => string.Equals(translation.Language.Culture, "tr-TR", StringComparison.OrdinalIgnoreCase))
            .Select(translation => translation.Title)
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(defaultTranslated)
            ? defaultTranslated.Trim()
            : congress.Name.Trim();
    }

    private string ResolveSubmissionTitle(Submission submission, CultureInfo culture)
    {
        bool english = culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        if (english && !string.IsNullOrWhiteSpace(submission.TitleEn))
            return submission.TitleEn.Trim();

        return string.IsNullOrWhiteSpace(submission.Title)
            ? ResolveSubmissionNumber(submission)
            : submission.Title.Trim();
    }

    private static string ResolveSubmissionNumber(Submission submission)
    {
        return string.IsNullOrWhiteSpace(submission.SubmissionNumber)
            ? submission.Id.ToString("N")[..8].ToUpperInvariant()
            : submission.SubmissionNumber.Trim();
    }

    private string ResolveSubmissionTypeName(Submission submission, CultureInfo culture)
    {
        if (submission.SubmissionType is null)
            return ResolveVideoPageLabels(culture).VideoPresentation;

        string? translated = submission.SubmissionType.Translations
            .Where(translation => translation.DeletedDate == null && string.Equals(translation.Language.Culture, culture.Name, StringComparison.OrdinalIgnoreCase))
            .Select(translation => translation.Name)
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(translated)
            ? translated.Trim()
            : submission.SubmissionType.Code ?? ResolveVideoPageLabels(culture).VideoPresentation;
    }

    private string ResolveAuthorsText(Submission submission, CultureInfo culture)
    {
        List<Author> authors = submission.Authors
            .Where(author => author.DeletedDate == null)
            .OrderByDescending(author => author.IsCorrespondingAuthor)
            .ThenBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .ToList();

        if (authors.Count == 0)
            return "-";

        return string.Join(Environment.NewLine, authors.Select(author => ResolveAuthorDisplayName(author, culture)));
    }

    private string ResolveAuthorsInlineText(Submission submission, CultureInfo culture)
    {
        List<string> authors = submission.Authors
            .Where(author => author.DeletedDate == null)
            .OrderByDescending(author => author.IsCorrespondingAuthor)
            .ThenBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .Select(author => ResolveAuthorDisplayName(author, culture))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(3)
            .ToList();

        if (authors.Count == 0)
            return "-";

        int totalAuthorCount = submission.Authors.Count(author => author.DeletedDate == null);
        string text = string.Join(", ", authors);
        if (totalAuthorCount > authors.Count)
            text = $"{text} +{totalAuthorCount - authors.Count}";

        return text;
    }

    private string ResolveAuthorDisplayName(Author author, CultureInfo culture)
    {
        string title = ResolveAuthorTitle(author, culture);
        string fullName = $"{author.FirstName} {author.LastName}".Trim();
        return string.IsNullOrWhiteSpace(title) ? fullName : $"{title} {fullName}".Trim();
    }

    private static string ResolveAuthorTitle(Author author, CultureInfo culture)
    {
        if (author.Title is null)
            return string.Empty;

        string? translated = author.Title.Translations
            .Where(translation => translation.DeletedDate == null && string.Equals(translation.Language.Culture, culture.Name, StringComparison.OrdinalIgnoreCase))
            .Select(translation => !string.IsNullOrWhiteSpace(translation.Description) ? translation.Description : translation.Name)
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(translated)
            ? translated.Trim()
            : author.Title.Code ?? string.Empty;
    }

    private static VideoPageLabels ResolveVideoPageLabels(CultureInfo culture)
    {
        bool english = culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        return english
            ? new VideoPageLabels(
                VideoPresentation: "Video Presentation",
                SubmissionDetails: "Submission Details",
                Authors: "Authors",
                SubmissionNumber: "Submission No",
                SubmissionType: "Submission Type",
                File: "File",
                RelatedVideos: "Other Video Presentations",
                SearchVideos: "Search by title, submission no or author...",
                VideoCount: "videos",
                NoRelatedVideos: "There are no other video presentations available for this congress.",
                SeparateTabText: "If the video does not open, open it in a separate tab.",
                BrowserNotSupported: "Your browser does not support video playback.",
                Language: "Language",
                Turkish: "Turkish",
                English: "English")
            : new VideoPageLabels(
                VideoPresentation: "Video Sunum",
                SubmissionDetails: "Bildiri Bilgileri",
                Authors: "Yazarlar",
                SubmissionNumber: "Bildiri No",
                SubmissionType: "Bildiri Türü",
                File: "Dosya",
                RelatedVideos: "Diğer Video Sunumlar",
                SearchVideos: "Başlık, bildiri no veya yazara göre ara...",
                VideoCount: "video",
                NoRelatedVideos: "Bu kongrede gösterilecek başka video sunumu bulunamadı.",
                SeparateTabText: "Video açılmazsa ayrı sekmede aç.",
                BrowserNotSupported: "Tarayıcınız video oynatmayı desteklemiyor.",
                Language: "Dil",
                Turkish: "Türkçe",
                English: "İngilizce");
    }

    private static string ResolveContentType(SubmissionFile file, string? storageContentType)
    {
        string extension = ResolveFileExtension(file);

        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".m4v" => "video/x-m4v",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ when IsSafeContentType(file.ContentType) => file.ContentType!.Trim(),
            _ when IsSafeContentType(storageContentType) => storageContentType!.Trim(),
            _ => "application/octet-stream"
        };
    }

    private static string ResolveFileExtension(SubmissionFile file)
    {
        string extension = Path.GetExtension(file.OriginalFileName).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(extension))
            return extension;

        string? filePath = file.FilePath;
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        string candidate = filePath.Trim();
        if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri? absoluteUri))
            candidate = absoluteUri.AbsolutePath;

        return Path.GetExtension(candidate).ToLowerInvariant();
    }

    private static bool IsSafeContentType(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType) &&
               !contentType.Contains('\r') &&
               !contentType.Contains('\n');
    }

    private static string BuildInlineContentDisposition(string? originalFileName, string? filePath)
    {
        string fileName = Path.GetFileName(originalFileName ?? string.Empty)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(filePath))
        {
            string candidate = filePath.Trim();
            if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri? absoluteUri))
                candidate = absoluteUri.AbsolutePath;

            fileName = Path.GetFileName(candidate)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "file";

        string asciiFallback = new(fileName
            .Select(character => character >= ' ' && character <= '~' && character != '"' && character != '\\'
                ? character
                : '_')
            .ToArray());

        string encodedFileName = Uri.EscapeDataString(fileName);
        return $"inline; filename=\"{asciiFallback}\"; filename*=UTF-8''{encodedFileName}";
    }

    private static bool IsExpired(ShortLink shortLink)
    {
        return shortLink.ExpiresAt.HasValue && shortLink.ExpiresAt.Value <= DateTime.UtcNow;
    }

    private string? ResolveSubmissionBucketName()
    {
        return string.IsNullOrWhiteSpace(_objectStorageOptions.Buckets.Submissions)
            ? null
            : _objectStorageOptions.Buckets.Submissions.Trim();
    }

    private static string NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : code.Trim();
    }

    private sealed record ResolvedShortLinkFile(
        ShortLink ShortLink,
        SubmissionFile File,
        string BucketName,
        string ObjectName,
        ObjectStorageFileInfo FileInfo);

    private readonly record struct ByteRange(
        long Start,
        long End,
        long TotalLength,
        bool IsPartial)
    {
        public long Length => End - Start + 1;
    }

    private sealed record VideoPageLabels(
        string VideoPresentation,
        string SubmissionDetails,
        string Authors,
        string SubmissionNumber,
        string SubmissionType,
        string File,
        string RelatedVideos,
        string SearchVideos,
        string VideoCount,
        string NoRelatedVideos,
        string SeparateTabText,
        string BrowserNotSupported,
        string Language,
        string Turkish,
        string English);

    private sealed record RelatedVideoPresentation(
        string Code,
        string Title,
        string SubmissionNumber,
        string SubmissionTypeName,
        string Authors);
}
