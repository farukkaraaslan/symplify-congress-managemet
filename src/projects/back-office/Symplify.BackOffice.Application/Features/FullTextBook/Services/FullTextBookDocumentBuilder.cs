using System.Globalization;
using Core.Application.Storage;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.AbstractBook.Services;
using Symplify.BackOffice.Application.Features.FullTextBook.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Services.Repositories;

namespace Symplify.BackOffice.Application.Features.FullTextBook.Services;

public sealed class FullTextBookDocumentBuilder : IFullTextBookDocumentBuilder
{
    private const long MaxSingleDocumentSizeInBytes = 50L * 1024 * 1024;
    private const long MaxCombinedDocumentSizeInBytes = 300L * 1024 * 1024;
    private const string FullTextFontName = "Times New Roman";
    private const string FullTextBodyFontSizeHalfPoints = "22";

    private readonly IAbstractBookDocumentBuilder _baseBookBuilder;
    private readonly IFullTextBookRepository _fullTextBookRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly ILogger<FullTextBookDocumentBuilder> _logger;

    public FullTextBookDocumentBuilder(
        IAbstractBookDocumentBuilder baseBookBuilder,
        IFullTextBookRepository fullTextBookRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions,
        ILogger<FullTextBookDocumentBuilder> logger)
    {
        _baseBookBuilder = baseBookBuilder;
        _fullTextBookRepository = fullTextBookRepository;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task<FullTextBookDocumentModel> BuildAsync(
        FullTextBookBuildRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CongressId == Guid.Empty)
            throw new InvalidOperationException("Kongre seçimi zorunludur.");

        IReadOnlyList<FullTextBookFileSourceDto> fileSources =
            await _fullTextBookRepository.GetLatestApprovedFilesAsync(
                request.CongressId,
                cancellationToken);

        if (fileSources.Count == 0)
        {
            throw new InvalidOperationException(
                "Seçilen kongreye ait onaylanmış Word tam metin dosyası bulunamadı.");
        }

        Guid[] submissionIds = fileSources
            .Select(source => source.SubmissionId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        AbstractBookOptionsDto options = CreateBookOptions(
            request.CoverImageBytes,
            request.CoverImageContentType);
        AbstractBookDocumentModel baseBook = await _baseBookBuilder.BuildAsync(
            new AbstractBookBuildRequest
            {
                CongressId = request.CongressId,
                Culture = request.Culture,
                Filter = new ProgramSubmissionFilterDto
                {
                    Preset = ProgramSubmissionScopePreset.AllActive,
                    IncludedSubmissionIds = submissionIds
                },
                Options = options
            },
            cancellationToken);

        IReadOnlyDictionary<Guid, FullTextBookFileSourceDto> sourceBySubmissionId = fileSources
            .GroupBy(source => source.SubmissionId)
            .ToDictionary(group => group.Key, group => group.First());

        List<AbstractBookEntryDto> eligibleEntries = baseBook.Entries
            .Where(entry => sourceBySubmissionId.ContainsKey(entry.Id))
            .ToList();

        if (eligibleEntries.Count == 0)
        {
            throw new InvalidOperationException(
                "Onaylı tam metin dosyalarıyla eşleşen aktif bildiri bulunamadı.");
        }

        string bucketName = _storageOptions.Buckets.Submissions?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException(
                "Tam metin kitabı için bildiri dosya depolama ayarı bulunamadı.");
        }

        List<FullTextBookDocumentDto> documents = new(eligibleEntries.Count);
        long combinedSize = 0;

        foreach (AbstractBookEntryDto entry in eligibleEntries)
        {
            FullTextBookFileSourceDto source = sourceBySubmissionId[entry.Id];
            ValidateSource(source);

            byte[] content = await ReadDocumentAsync(
                bucketName,
                source,
                entry,
                cancellationToken);

            ValidateDocumentContent(source, entry, content);
            content = PrepareDocumentForMerge(source, entry, content);

            combinedSize += content.LongLength;
            if (combinedSize > MaxCombinedDocumentSizeInBytes)
            {
                throw new InvalidOperationException(
                    "Birleştirilecek tam metin dosyalarının toplam boyutu 300 MB sınırını aşıyor.");
            }

            documents.Add(new FullTextBookDocumentDto
            {
                SubmissionId = entry.Id,
                SubmissionNumber = entry.SubmissionNumber,
                Title = ResolveTitle(entry, options),
                OriginalFileName = source.OriginalFileName,
                Content = content
            });
        }

        AbstractBookDocumentModel filteredBaseBook = new()
        {
            CongressId = baseBook.CongressId,
            CongressCode = baseBook.CongressCode,
            CongressName = baseBook.CongressName,
            CongressEnglishName = baseBook.CongressEnglishName,
            CongressSubtitle = baseBook.CongressSubtitle,
            StartDate = baseBook.StartDate,
            EndDate = baseBook.EndDate,
            Venue = baseBook.Venue,
            City = baseBook.City,
            Options = CloneOptions(
                baseBook.Options,
                (baseBook.StartDate?.Year ?? DateTime.UtcNow.Year).ToString(CultureInfo.InvariantCulture)),
            Boards = baseBook.Boards,
            Entries = eligibleEntries
        };

        return new FullTextBookDocumentModel
        {
            BaseBook = filteredBaseBook,
            FullTextDocuments = documents
        };
    }

    private async Task<byte[]> ReadDocumentAsync(
        string bucketName,
        FullTextBookFileSourceDto source,
        AbstractBookEntryDto entry,
        CancellationToken cancellationToken)
    {
        try
        {
            await using Stream objectStream = await _objectStorageService.OpenReadAsync(
                bucketName,
                source.FilePath.Trim(),
                cancellationToken);
            using MemoryStream buffer = new();
            await objectStream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Approved full text could not be read for full text book. FileId: {FileId}, SubmissionId: {SubmissionId}, ObjectName: {ObjectName}",
                source.FileId,
                source.SubmissionId,
                source.FilePath);

            throw new InvalidOperationException(
                $"{entry.SubmissionNumber} numaralı bildirinin tam metin dosyası okunamadı.",
                exception);
        }
    }

    private static AbstractBookOptionsDto CreateBookOptions(
        byte[]? coverImageBytes,
        string? coverImageContentType)
        => new()
        {
            IncludeCover = true,
            IncludePublicationInfo = true,
            IncludeBoards = true,
            IncludeTableOfContents = true,
            StartEachSubmissionOnNewPage = true,
            IncludeTurkishContent = true,
            IncludeEnglishContent = true,
            IncludeOrcid = true,
            IncludeInstitutions = true,
            IncludeCorrespondingAuthor = true,
            SortMode = AbstractBookSortMode.SubmissionNumber,
            CoverTheme = AbstractBookCoverTheme.Corporate,
            BookTitle = "Tam Metin Kitabı",
            EnglishBookTitle = "Full Text Book",
            CoverImageBytes = coverImageBytes,
            CoverImageContentType = coverImageContentType
        };

    private static AbstractBookOptionsDto CloneOptions(
        AbstractBookOptionsDto source,
        string publicationYear)
        => new()
        {
            IncludeCover = source.IncludeCover,
            IncludePublicationInfo = source.IncludePublicationInfo,
            IncludeBoards = source.IncludeBoards,
            IncludeTableOfContents = source.IncludeTableOfContents,
            StartEachSubmissionOnNewPage = source.StartEachSubmissionOnNewPage,
            IncludeTurkishContent = source.IncludeTurkishContent,
            IncludeEnglishContent = source.IncludeEnglishContent,
            IncludeOrcid = source.IncludeOrcid,
            IncludeInstitutions = source.IncludeInstitutions,
            IncludeCorrespondingAuthor = source.IncludeCorrespondingAuthor,
            SortMode = source.SortMode,
            CoverTheme = source.CoverTheme,
            BookTitle = source.BookTitle,
            EnglishBookTitle = source.EnglishBookTitle,
            Editor = source.Editor,
            Isbn = source.Isbn,
            PublicationYear = publicationYear,
            City = source.City,
            PublishingDirector = source.PublishingDirector,
            CoverDesigner = source.CoverDesigner,
            InteriorDesigner = source.InteriorDesigner,
            Publisher = source.Publisher,
            EditionInformation = source.EditionInformation,
            PublisherAddress = source.PublisherAddress,
            PublisherEmail = source.PublisherEmail,
            PublisherWebsite = source.PublisherWebsite,
            CoverImageBytes = source.CoverImageBytes,
            CoverImageContentType = source.CoverImageContentType,
            HeaderLogoBytes = source.HeaderLogoBytes,
            HeaderLogoContentType = source.HeaderLogoContentType
        };

    private static void ValidateSource(FullTextBookFileSourceDto source)
    {
        if (string.IsNullOrWhiteSpace(source.FilePath))
            throw new InvalidOperationException("Onaylı tam metin dosyasının depolama yolu bulunamadı.");

        if (BackOfficeObjectStorageHelper.IsExternalOrLegacyLocalPath(source.FilePath))
        {
            throw new InvalidOperationException(
                $"{source.OriginalFileName} eski veya harici bir yolda olduğu için kitaba eklenemedi.");
        }

        string extension = Path.GetExtension(source.OriginalFileName);
        if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{source.OriginalFileName} Word DOCX formatında değil. Tam metin kitabına yalnızca .docx dosyaları eklenebilir.");
        }

        if (source.FileSize is > MaxSingleDocumentSizeInBytes)
        {
            throw new InvalidOperationException(
                $"{source.OriginalFileName} 50 MB dosya sınırını aşıyor.");
        }
    }

    private static void ValidateDocumentContent(
        FullTextBookFileSourceDto source,
        AbstractBookEntryDto entry,
        byte[] content)
    {
        if (content.Length == 0)
        {
            throw new InvalidOperationException(
                $"{entry.SubmissionNumber} numaralı bildirinin tam metin dosyası boş.");
        }

        if (content.LongLength > MaxSingleDocumentSizeInBytes)
        {
            throw new InvalidOperationException(
                $"{source.OriginalFileName} 50 MB dosya sınırını aşıyor.");
        }

        try
        {
            using MemoryStream stream = new(content, writable: false);
            using WordprocessingDocument document = WordprocessingDocument.Open(stream, false);
            if (document.MainDocumentPart?.Document?.Body is null)
                throw new InvalidDataException("DOCX ana belge bölümü bulunamadı.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"{entry.SubmissionNumber} numaralı bildirinin tam metin dosyası geçerli bir DOCX belgesi değil.",
                exception);
        }
    }

    private static byte[] PrepareDocumentForMerge(
        FullTextBookFileSourceDto source,
        AbstractBookEntryDto entry,
        byte[] content)
    {
        try
        {
            using MemoryStream stream = new();
            stream.Write(content, 0, content.Length);
            stream.Position = 0;

            using (WordprocessingDocument document = WordprocessingDocument.Open(stream, true))
            {
                W.Body? body = document.MainDocumentPart?.Document?.Body;
                if (body is null)
                    throw new InvalidDataException("DOCX ana belge bölümü bulunamadı.");

                // Kaynak belgenin section/page-number ayarları ana kitabın sayfalamasını
                // bozmamalıdır. Özellikle OddPage/EvenPage section'ları boş sayfa üretir.
                foreach (W.SectionProperties section in body
                             .Descendants<W.SectionProperties>()
                             .ToList())
                {
                    section.Remove();
                }

                foreach (W.LastRenderedPageBreak renderedPageBreak in body
                             .Descendants<W.LastRenderedPageBreak>()
                             .ToList())
                {
                    renderedPageBreak.Remove();
                }

                RemoveTrailingMergeArtifacts(body);
                ApplyFullTextTypography(document);

                // Tam metin, bildiri üst bilgi bloğunun hemen altında başlamalıdır.
                // Kaynak dosyanın ilk paragrafında zorunlu sayfa başlatma varsa kaldırılır;
                // sonraki bildiriye geçişi ana kitap renderer'ı yönetir.
                W.Paragraph? firstParagraph = body.Elements<W.Paragraph>().FirstOrDefault();
                if (firstParagraph is not null)
                {
                    firstParagraph.ParagraphProperties?.RemoveAllChildren<W.PageBreakBefore>();
                    foreach (W.Break pageBreak in firstParagraph
                                 .Descendants<W.Break>()
                                 .Where(item => item.Type?.Value == W.BreakValues.Page)
                                 .ToList())
                    {
                        pageBreak.Remove();
                    }
                }

                document.MainDocumentPart!.Document.Save();
            }

            return stream.ToArray();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"{entry.SubmissionNumber} numaralı bildirinin tam metin dosyası birleştirme için hazırlanamadı ({source.OriginalFileName}).",
                exception);
        }
    }

    private static void ApplyFullTextTypography(WordprocessingDocument document)
    {
        MainDocumentPart mainPart = document.MainDocumentPart
            ?? throw new InvalidDataException("DOCX ana belge bölümü bulunamadı.");

        HashSet<string> headingStyleIds = ResolveHeadingStyleIds(mainPart);
        ApplyStyleTypography(mainPart, headingStyleIds);

        if (mainPart.Document is not null)
            ApplyPartTypography(mainPart.Document, headingStyleIds);

        if (mainPart.FootnotesPart?.Footnotes is not null)
            ApplyPartTypography(mainPart.FootnotesPart.Footnotes, headingStyleIds);

        if (mainPart.EndnotesPart?.Endnotes is not null)
            ApplyPartTypography(mainPart.EndnotesPart.Endnotes, headingStyleIds);

        if (mainPart.WordprocessingCommentsPart?.Comments is not null)
            ApplyPartTypography(mainPart.WordprocessingCommentsPart.Comments, headingStyleIds);
    }

    private static HashSet<string> ResolveHeadingStyleIds(MainDocumentPart mainPart)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase)
        {
            "Title",
            "Subtitle",
            "Heading1",
            "Heading2",
            "Heading3",
            "Heading4",
            "Heading5",
            "Heading6",
            "Heading7",
            "Heading8",
            "Heading9"
        };

        W.Styles? styles = mainPart.StyleDefinitionsPart?.Styles;
        if (styles is null)
            return result;

        foreach (W.Style style in styles.Elements<W.Style>())
        {
            string styleId = style.StyleId?.Value?.Trim() ?? string.Empty;
            string styleName = style.StyleName?.Val?.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(styleId))
                continue;

            if (style.StyleParagraphProperties?.OutlineLevel is not null
                || LooksLikeHeadingName(styleId)
                || LooksLikeHeadingName(styleName))
            {
                result.Add(styleId);
            }
        }

        return result;
    }

    private static bool LooksLikeHeadingName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = value
            .Replace("ı", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ş", "s", StringComparison.OrdinalIgnoreCase)
            .Replace("ğ", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "u", StringComparison.OrdinalIgnoreCase)
            .Replace("ö", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ç", "c", StringComparison.OrdinalIgnoreCase);

        return normalized.Contains("heading", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("title", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("baslik", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("subtitle", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyStyleTypography(
        MainDocumentPart mainPart,
        IReadOnlySet<string> headingStyleIds)
    {
        W.Styles? styles = mainPart.StyleDefinitionsPart?.Styles;
        if (styles is null)
            return;

        foreach (W.Style style in styles.Elements<W.Style>())
        {
            W.StyleRunProperties runProperties = style.StyleRunProperties
                ?? style.AppendChild(new W.StyleRunProperties());
            SetFontFamily(runProperties);

            bool isParagraphStyle = style.Type?.Value == W.StyleValues.Paragraph;
            string styleId = style.StyleId?.Value ?? string.Empty;
            if (isParagraphStyle && !headingStyleIds.Contains(styleId))
                SetFontSize(runProperties, FullTextBodyFontSizeHalfPoints);
        }

        styles.Save();
    }

    private static void ApplyPartTypography(
        OpenXmlElement root,
        IReadOnlySet<string> headingStyleIds)
    {
        foreach (W.Paragraph paragraph in root.Descendants<W.Paragraph>())
        {
            bool isHeading = IsHeadingParagraph(paragraph, headingStyleIds);

            foreach (W.Run run in paragraph.Descendants<W.Run>())
            {
                W.RunProperties runProperties = run.RunProperties
                    ?? run.PrependChild(new W.RunProperties());
                SetFontFamily(runProperties);
                if (!isHeading)
                    SetFontSize(runProperties, FullTextBodyFontSizeHalfPoints);
            }
        }
    }

    private static bool IsHeadingParagraph(
        W.Paragraph paragraph,
        IReadOnlySet<string> headingStyleIds)
    {
        W.ParagraphProperties? properties = paragraph.ParagraphProperties;
        if (properties?.OutlineLevel is not null)
            return true;

        string styleId = properties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(styleId) && headingStyleIds.Contains(styleId))
            return true;

        string text = paragraph.InnerText?.Trim() ?? string.Empty;
        if (text.Length == 0 || text.Length > 300)
            return false;

        int maxFontSize = paragraph.Descendants<W.FontSize>()
            .Select(fontSize => int.TryParse(fontSize.Val?.Value, out int value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();

        return maxFontSize > 22;
    }

    private static void SetFontFamily(W.RunProperties properties)
        => properties.RunFonts = CreateFullTextRunFonts();

    private static void SetFontFamily(W.StyleRunProperties properties)
        => properties.RunFonts = CreateFullTextRunFonts();

    private static W.RunFonts CreateFullTextRunFonts()
        => new()
        {
            Ascii = new StringValue(FullTextFontName),
            HighAnsi = new StringValue(FullTextFontName),
            EastAsia = new StringValue(FullTextFontName),
            ComplexScript = new StringValue(FullTextFontName)
        };

    private static void SetFontSize(
        W.RunProperties properties,
        string halfPoints)
    {
        properties.FontSize = new W.FontSize { Val = new StringValue(halfPoints) };
        properties.FontSizeComplexScript = new W.FontSizeComplexScript
        {
            Val = new StringValue(halfPoints)
        };
    }

    private static void SetFontSize(
        W.StyleRunProperties properties,
        string halfPoints)
    {
        properties.FontSize = new W.FontSize { Val = new StringValue(halfPoints) };
        properties.FontSizeComplexScript = new W.FontSizeComplexScript
        {
            Val = new StringValue(halfPoints)
        };
    }

    private static void RemoveTrailingMergeArtifacts(W.Body body)
    {
        while (body.LastChild is W.Paragraph paragraph
               && IsEmptyMergeParagraph(paragraph))
        {
            paragraph.Remove();
        }

        W.Paragraph? lastParagraph = body.Elements<W.Paragraph>().LastOrDefault();
        if (lastParagraph is null)
            return;

        lastParagraph.ParagraphProperties?.RemoveAllChildren<W.PageBreakBefore>();

        // A page break placed after the final visible text creates an empty page once
        // the host renderer adds the next submission's page break.
        List<W.Break> trailingBreaks = lastParagraph
            .Descendants<W.Break>()
            .Where(item => item.Type?.Value == W.BreakValues.Page)
            .ToList();

        if (string.IsNullOrWhiteSpace(lastParagraph.InnerText))
        {
            foreach (W.Break pageBreak in trailingBreaks)
                pageBreak.Remove();
        }
    }

    private static bool IsEmptyMergeParagraph(W.Paragraph paragraph)
    {
        if (!string.IsNullOrWhiteSpace(paragraph.InnerText))
            return false;

        return !paragraph.Descendants().Any(element =>
            element.LocalName is "drawing" or "pict" or "object");
    }

    private static string ResolveTitle(
        AbstractBookEntryDto entry,
        AbstractBookOptionsDto options)
    {
        if (options.IncludeTurkishContent)
            return FirstNonEmpty(entry.TurkishTitle, entry.EnglishTitle, entry.SubmissionNumber);

        return FirstNonEmpty(entry.EnglishTitle, entry.TurkishTitle, entry.SubmissionNumber);
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
