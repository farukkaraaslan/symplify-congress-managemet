using System.Globalization;
using System.IO.Compression;
using System.Threading;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using O = DocumentFormat.OpenXml;
using P = DocumentFormat.OpenXml.Packaging;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.FullTextBook.Models;
using Symplify.BackOffice.Application.Features.FullTextBook.Services;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Services;

public sealed class AbstractBookWordRenderer : IAbstractBookWordRenderer, IFullTextBookWordRenderer
{
    private static int _drawingIdSeed;
    private const string FontName = "Times New Roman";
    private const string Heading1StyleId = "Heading1";
    private const string Heading2StyleId = "Heading2";
    private const string BookTitleStyleId = "BookTitle";
    private const string BookSubtitleStyleId = "BookSubtitle";
    private const string SmallMutedStyleId = "SmallMuted";

    public byte[] Render(AbstractBookDocumentModel model, string? culture)
        => RenderCore(model, fullTextDocuments: null, culture: culture);

    public byte[] Render(FullTextBookDocumentModel model, string? culture)
    {
        ArgumentNullException.ThrowIfNull(model);
        return RenderCore(model.BaseBook, model.FullTextDocuments, culture);
    }

    private static byte[] RenderCore(
        AbstractBookDocumentModel model,
        IReadOnlyList<FullTextBookDocumentDto>? fullTextDocuments,
        string? culture)
    {
        ArgumentNullException.ThrowIfNull(model);

        bool isFullTextBook = fullTextDocuments is not null;
        if (model.Entries.Count == 0)
        {
            throw new InvalidOperationException(
                isFullTextBook
                    ? "Tam metin kitabı için bildiri bulunamadı."
                    : "Özet kitabı için bildiri bulunamadı.");
        }

        IReadOnlyList<FullTextBookDocumentDto> fullTexts = fullTextDocuments
            ?? Array.Empty<FullTextBookDocumentDto>();

        WordTheme theme = ResolveTheme(model.Options.CoverTheme);
        using MemoryStream stream = new();

        using (P.WordprocessingDocument package = P.WordprocessingDocument.Create(
                   stream,
                   O.WordprocessingDocumentType.Document,
                   true))
        {
            P.MainDocumentPart mainPart = package.AddMainDocumentPart();
            mainPart.Document = new W.Document();
            W.Body body = mainPart.Document.AppendChild(new W.Body());

            AddStyles(mainPart, theme);
            AddSettings(
                mainPart,
                updateFieldsOnOpen: model.Options.IncludeTableOfContents);
            string footerRelationshipId = AddFooter(mainPart);
            string headerRelationshipId = AddHeader(mainPart, model, theme, culture);
            bool hasFrontSection = false;

            if (model.Options.IncludeCover)
            {
                RenderCover(body, mainPart, model, theme);
                // Kapak, son kapak paragrafına bağlı ayrı ve kenar boşluksuz bir section olarak kapanır.
                // Böylece kapak resmi ile section break arasında fazladan boş paragraf oluşmaz.
                ApplySectionPropertiesToLastParagraph(body, CreateCoverSectionProperties());
            }

            // Ön bölüm sırası sabittir: Kapak -> İçindekiler -> Kurullar -> Bildiriler.
            if (model.Options.IncludeTableOfContents)
            {
                if (isFullTextBook)
                    RenderFullTextContents(body, model, fullTexts, theme);
                else
                    RenderContents(body, model, theme);

                hasFrontSection = true;
            }

            if (model.Options.IncludeBoards && model.Boards.Count > 0)
            {
                if (hasFrontSection)
                    AddPageBreakBefore(body);
                RenderBoards(body, model.Boards, theme);
                hasFrontSection = true;
            }

            if (isFullTextBook)
            {
                if (hasFrontSection)
                    AddPageBreakBefore(body);

                RenderProceedingsDividerPage(body, theme);
                AddPageBreakBefore(body);
                RenderFullTextEntries(body, mainPart, model, fullTexts, theme);
            }
            else
            {
                if (hasFrontSection)
                    AddPageBreakBefore(body);

                RenderEntries(
                    body,
                    mainPart,
                    model,
                    theme,
                    includeBookmarks: model.Options.IncludeTableOfContents);
            }

            body.Append(CreateSectionProperties(footerRelationshipId, headerRelationshipId));
            mainPart.Document.Save();
        }

        return NormalizeDocxPackage(stream.ToArray());
    }

    private static byte[] NormalizeDocxPackage(byte[] packageBytes)
    {
        using MemoryStream stream = new();
        stream.Write(packageBytes, 0, packageBytes.Length);
        stream.Position = 0;

        using (ZipArchive archive = new(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            NormalizeContentTypes(archive);
            NormalizeRootRelationships(archive);
            NormalizeWordPartRelationshipsAndMedia(archive);
            NormalizeOpenXmlOrdering(archive);
        }

        return stream.ToArray();
    }

    private static void NormalizeContentTypes(ZipArchive archive)
    {
        ZipArchiveEntry? contentTypesEntry = archive.GetEntry("[Content_Types].xml");
        if (contentTypesEntry is null)
            throw new InvalidOperationException("DOCX content types manifest could not be found.");

        XDocument contentTypes;
        using (Stream entryStream = contentTypesEntry.Open())
            contentTypes = XDocument.Load(entryStream);

        XNamespace ns = "http://schemas.openxmlformats.org/package/2006/content-types";
        XElement root = contentTypes.Root
            ?? throw new InvalidOperationException("DOCX content types manifest could not be read.");

        root.Elements(ns + "Default")
            .Where(element => string.Equals(
                (string?)element.Attribute("Extension"),
                "xml",
                StringComparison.OrdinalIgnoreCase))
            .Remove();

        EnsureContentTypeOverride(
            root, ns, "/word/document.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml");

        EnsureContentTypeOverrideIfPartExists(
            archive, root, ns, "word/styles.xml",
            "/word/styles.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml");
        EnsureContentTypeOverrideIfPartExists(
            archive, root, ns, "word/settings.xml",
            "/word/settings.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml");

        foreach (ZipArchiveEntry entry in archive.Entries
                     .Where(entry => entry.FullName.StartsWith("word/header", StringComparison.OrdinalIgnoreCase)
                                     && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            EnsureContentTypeOverride(
                root, ns, $"/{entry.FullName}",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml");
        }

        foreach (ZipArchiveEntry entry in archive.Entries
                     .Where(entry => entry.FullName.StartsWith("word/footer", StringComparison.OrdinalIgnoreCase)
                                     && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            EnsureContentTypeOverride(
                root, ns, $"/{entry.FullName}",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml");
        }

        ReplaceXmlEntry(archive, "[Content_Types].xml", contentTypes);
    }

    private static void EnsureContentTypeOverrideIfPartExists(
        ZipArchive archive,
        XElement root,
        XNamespace ns,
        string entryName,
        string partName,
        string contentType)
    {
        if (archive.GetEntry(entryName) is not null)
            EnsureContentTypeOverride(root, ns, partName, contentType);
    }

    private static void NormalizeRootRelationships(ZipArchive archive)
    {
        NormalizeRelationshipEntry(archive, "_rels/.rels", target =>
        {
            if (string.Equals(target, "/word/document.xml", StringComparison.OrdinalIgnoreCase))
                return "word/document.xml";

            return target.StartsWith("/", StringComparison.Ordinal)
                ? target[1..]
                : target;
        });
    }

    private static void NormalizeWordPartRelationshipsAndMedia(ZipArchive archive)
    {
        HashSet<(string Source, string Destination)> mediaMoves = new();
        List<string> relationshipEntries = archive.Entries
            .Where(entry => entry.FullName.StartsWith("word/_rels/", StringComparison.OrdinalIgnoreCase)
                            && entry.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.FullName)
            .ToList();

        foreach (string relationshipEntry in relationshipEntries)
        {
            NormalizeRelationshipEntry(archive, relationshipEntry, target =>
            {
                string normalized = target.Replace('\\', '/');

                if (normalized.StartsWith("/word/", StringComparison.OrdinalIgnoreCase))
                    normalized = normalized[6..];
                else if (normalized.StartsWith("word/", StringComparison.OrdinalIgnoreCase))
                    normalized = normalized[5..];

                if (normalized.StartsWith("/media/", StringComparison.OrdinalIgnoreCase)
                    || normalized.StartsWith("../media/", StringComparison.OrdinalIgnoreCase))
                {
                    string fileName = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
                    mediaMoves.Add(($"media/{fileName}", $"word/media/{fileName}"));
                    normalized = $"media/{fileName}";
                }

                return normalized.TrimStart('/');
            });
        }

        foreach ((string source, string destination) in mediaMoves)
            MoveZipEntry(archive, source, destination);
    }

    private static void NormalizeOpenXmlOrdering(ZipArchive archive)
    {
        List<string> entries = archive.Entries
            .Where(entry => string.Equals(entry.FullName, "word/document.xml", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(entry.FullName, "word/styles.xml", StringComparison.OrdinalIgnoreCase)
                            || entry.FullName.StartsWith("word/header", StringComparison.OrdinalIgnoreCase)
                               && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                            || entry.FullName.StartsWith("word/footer", StringComparison.OrdinalIgnoreCase)
                               && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (string entry in entries)
            NormalizeOpenXmlOrderingForEntry(archive, entry);
    }

    private static void NormalizeOpenXmlOrderingForEntry(ZipArchive archive, string entryName)
    {
        ZipArchiveEntry? entry = archive.GetEntry(entryName);
        if (entry is null)
            return;

        XDocument document;
        using (Stream entryStream = entry.Open())
            document = XDocument.Load(entryStream, LoadOptions.PreserveWhitespace);

        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        NormalizeChildren(document, w + "pPr", new[]
        {
            "pStyle", "keepNext", "keepLines", "pageBreakBefore", "framePr",
            "widowControl", "numPr", "suppressLineNumbers", "pBdr", "shd",
            "tabs", "suppressAutoHyphens", "kinsoku", "wordWrap", "overflowPunct",
            "topLinePunct", "autoSpaceDE", "autoSpaceDN", "bidi", "adjustRightInd",
            "snapToGrid", "spacing", "ind", "contextualSpacing", "mirrorIndents",
            "suppressOverlap", "jc", "textDirection", "textAlignment", "textboxTightWrap",
            "outlineLvl", "divId", "cnfStyle", "rPr", "sectPr", "pPrChange"
        });

        NormalizeChildren(document, w + "rPr", new[]
        {
            "rStyle", "rFonts", "b", "bCs", "i", "iCs", "caps", "smallCaps",
            "strike", "dstrike", "outline", "shadow", "emboss", "imprint", "noProof",
            "snapToGrid", "vanish", "webHidden", "color", "spacing", "w", "kern",
            "position", "sz", "szCs", "highlight", "u", "effect", "bdr", "shd",
            "fitText", "vertAlign", "rtl", "cs", "em", "lang", "eastAsianLayout",
            "specVanish", "oMath", "rPrChange"
        });

        NormalizeChildren(document, w + "tblPr", new[]
        {
            "tblStyle", "tblpPr", "tblOverlap", "bidiVisual", "tblStyleRowBandSize",
            "tblStyleColBandSize", "tblW", "jc", "tblCellSpacing", "tblInd",
            "tblBorders", "shd", "tblLayout", "tblCellMar", "tblLook",
            "tblCaption", "tblDescription", "tblPrChange"
        });

        NormalizeChildren(document, w + "tcPr", new[]
        {
            "cnfStyle", "tcW", "gridSpan", "hMerge", "vMerge", "tcBorders",
            "shd", "noWrap", "tcMar", "textDirection", "tcFitText", "vAlign",
            "hideMark", "headers", "cellIns", "cellDel", "cellMerge", "tcPrChange"
        });

        NormalizeChildren(document, w + "sectPr", new[]
        {
            "headerReference", "footerReference", "footnotePr", "endnotePr", "type",
            "pgSz", "pgMar", "paperSrc", "pgBorders", "lnNumType", "pgNumType",
            "cols", "formProt", "vAlign", "noEndnote", "titlePg", "textDirection",
            "bidi", "rtlGutter", "docGrid", "printerSettings", "sectPrChange"
        });

        ReplaceXmlEntry(archive, entryName, document);
    }

    private static void NormalizeChildren(
        XDocument document,
        XName parentName,
        IReadOnlyList<string> expectedOrder)
    {
        Dictionary<string, int> orderMap = expectedOrder
            .Select((name, index) => new { name, index })
            .ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal);

        foreach (XElement parent in document.Descendants(parentName).ToList())
        {
            List<XElement> sortableElements = parent.Elements()
                .Where(element => orderMap.ContainsKey(element.Name.LocalName))
                .ToList();

            if (sortableElements.Count < 2)
                continue;

            List<XElement> sortedElements = sortableElements
                .OrderBy(element => orderMap[element.Name.LocalName])
                .ToList();

            if (sortableElements.SequenceEqual(sortedElements))
                continue;

            foreach (XElement element in sortableElements)
                element.Remove();

            foreach (XElement element in sortedElements)
                parent.Add(element);
        }
    }

    private static void NormalizeRelationshipEntry(
        ZipArchive archive,
        string entryName,
        Func<string, string> targetNormalizer)
    {
        ZipArchiveEntry? entry = archive.GetEntry(entryName);
        if (entry is null)
            return;

        XDocument document;
        using (Stream stream = entry.Open())
            document = XDocument.Load(stream);

        XNamespace ns = "http://schemas.openxmlformats.org/package/2006/relationships";
        bool changed = false;

        foreach (XElement relationship in document.Descendants(ns + "Relationship"))
        {
            string? targetMode = (string?)relationship.Attribute("TargetMode");
            if (string.Equals(targetMode, "External", StringComparison.OrdinalIgnoreCase))
                continue;

            string? target = (string?)relationship.Attribute("Target");
            if (string.IsNullOrWhiteSpace(target))
                continue;

            string normalizedTarget = targetNormalizer(target);
            if (string.Equals(target, normalizedTarget, StringComparison.Ordinal))
                continue;

            relationship.SetAttributeValue("Target", normalizedTarget);
            changed = true;
        }

        if (changed)
            ReplaceXmlEntry(archive, entryName, document);
    }

    private static void MoveZipEntry(ZipArchive archive, string sourceName, string destinationName)
    {
        ZipArchiveEntry? sourceEntry = archive.GetEntry(sourceName);
        if (sourceEntry is null)
            return;

        byte[] bytes;
        using (Stream sourceStream = sourceEntry.Open())
        using (MemoryStream buffer = new())
        {
            sourceStream.CopyTo(buffer);
            bytes = buffer.ToArray();
        }

        archive.GetEntry(destinationName)?.Delete();
        ZipArchiveEntry destinationEntry = archive.CreateEntry(destinationName, CompressionLevel.Optimal);
        using (Stream destinationStream = destinationEntry.Open())
            destinationStream.Write(bytes, 0, bytes.Length);

        sourceEntry.Delete();
    }

    private static void ReplaceXmlEntry(ZipArchive archive, string entryName, XDocument document)
    {
        archive.GetEntry(entryName)?.Delete();
        ZipArchiveEntry newEntry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using Stream outputStream = newEntry.Open();
        document.Save(outputStream, SaveOptions.DisableFormatting);
    }

    private static void EnsureContentTypeOverride(
        XElement root,
        XNamespace ns,
        string partName,
        string contentType)
    {
        XElement? existing = root.Elements(ns + "Override")
            .FirstOrDefault(element => string.Equals(
                (string?)element.Attribute("PartName"),
                partName,
                StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            root.Add(new XElement(
                ns + "Override",
                new XAttribute("PartName", partName),
                new XAttribute("ContentType", contentType)));
            return;
        }

        existing.SetAttributeValue("ContentType", contentType);
    }

    private static void RenderCover(
        W.Body body,
        P.MainDocumentPart mainPart,
        AbstractBookDocumentModel model,
        WordTheme theme)
    {
        if (model.Options.CoverImageBytes is { Length: > 0 })
        {
            W.Paragraph coverParagraph = new(
                new W.ParagraphProperties(
                    new W.SpacingBetweenLines
                    {
                        Before = "0",
                        After = "0"
                    },
                    new W.Justification
                    {
                        Val = new O.EnumValue<W.JustificationValues>(W.JustificationValues.Center)
                    }),
                new W.Run(CreateInlineCoverImageDrawing(
                    mainPart,
                    model.Options.CoverImageBytes,
                    model.Options.CoverImageContentType,
                    model.Options.CropCoverImageToFill)));
            body.Append(coverParagraph);
            return;
        }

        W.Table cover = CreateTable(new[] { 10000 }, borderless: true);
        W.TableCell cell = new();
        W.TableCellProperties cellProperties = CreateCellProperties(10000, theme.CoverBackground, borderless: true);
        cell.Append(cellProperties);

        cell.Append(CreateParagraph(
            model.CongressCode,
            SmallMutedStyleId,
            W.JustificationValues.Left,
            before: 1000,
            after: 260,
            color: theme.CoverMuted));
        cell.Append(CreateParagraph(
            model.CongressName,
            BookTitleStyleId,
            W.JustificationValues.Left,
            after: 220,
            color: theme.CoverText));

        if (!string.IsNullOrWhiteSpace(model.CongressSubtitle))
        {
            cell.Append(CreateParagraph(
                model.CongressSubtitle,
                justification: W.JustificationValues.Left,
                fontSizeHalfPoints: 24,
                color: theme.CoverMuted,
                after: 520));
        }

        cell.Append(CreateParagraph(
            FirstNonEmpty(model.Options.EnglishBookTitle, "ABSTRACT BOOK").ToUpperInvariant(),
            BookSubtitleStyleId,
            W.JustificationValues.Left,
            after: 80,
            color: theme.Accent));
        cell.Append(CreateParagraph(
            FirstNonEmpty(model.Options.BookTitle, "Özet Kitabı"),
            BookTitleStyleId,
            W.JustificationValues.Left,
            after: 560,
            color: theme.CoverText));

        string dateText = FormatDateRange(model.StartDate, model.EndDate);
        string location = string.Join(" - ", new[] { model.City, model.Venue }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        string eventLine = string.Join("\n", new[] { dateText, location }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(eventLine))
        {
            cell.Append(CreateParagraph(
                eventLine,
                justification: W.JustificationValues.Left,
                fontSizeHalfPoints: 22,
                color: theme.CoverMuted,
                after: 500));
        }

        if (!string.IsNullOrWhiteSpace(model.Options.Editor))
        {
            cell.Append(CreateParagraph(
                $"Editör / Editor\n{model.Options.Editor}",
                justification: W.JustificationValues.Left,
                fontSizeHalfPoints: 19,
                color: theme.CoverMuted,
                after: 220));
        }

        if (!string.IsNullOrWhiteSpace(model.Options.Isbn))
        {
            cell.Append(CreateParagraph(
                $"ISBN: {model.Options.Isbn}",
                justification: W.JustificationValues.Left,
                fontSizeHalfPoints: 17,
                color: theme.CoverMuted,
                after: 900));
        }
        else
        {
            cell.Append(CreateParagraph(string.Empty, before: 700, after: 300));
        }

        cover.Append(CreateTableRow(new[] { cell }));
        body.Append(cover);
    }

    private static void RenderPublicationInfo(W.Body body, AbstractBookDocumentModel model, WordTheme theme)
    {
        body.Append(CreateHeading("YAYIN KÜNYESİ / PUBLICATION INFORMATION", Heading1StyleId, theme));

        List<(string Label, string Value)> rows = new()
        {
            ("Yayın Yönetmeni / Publishing Director", model.Options.PublishingDirector ?? string.Empty),
            ("Editör / Editor", model.Options.Editor ?? string.Empty),
            ("Kapak Tasarımı / Cover Design", model.Options.CoverDesigner ?? string.Empty),
            ("İç Tasarım / Interior Design", model.Options.InteriorDesigner ?? string.Empty),
            ("ISBN", model.Options.Isbn ?? string.Empty),
            ("Yayınevi / Publisher", model.Options.Publisher ?? string.Empty),
            ("Baskı / Edition", model.Options.EditionInformation ?? string.Empty),
            ("Adres / Address", model.Options.PublisherAddress ?? string.Empty),
            ("E-posta / E-mail", model.Options.PublisherEmail ?? string.Empty),
            ("Web", model.Options.PublisherWebsite ?? string.Empty),
            ("Yayın Yılı / Publication Year", model.Options.PublicationYear ?? string.Empty)
        };

        List<(string Label, string Value)> printableRows = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToList();

        if (printableRows.Count == 0)
        {
            body.Append(CreateParagraph(
                "Yayın künyesi bilgisi girilmedi. / No publication information was entered.",
                SmallMutedStyleId,
                W.JustificationValues.Center,
                before: 260));
            return;
        }

        W.Table table = CreateTable(new[] { 3800, 6200 });
        foreach ((string label, string value) in printableRows)
        {
            table.Append(CreateTableRow(new[]
            {
                CreateTableCell(label, 3800, bold: true, background: theme.SoftBackground),
                CreateTableCell(value, 6200)
            }));
        }
        body.Append(table);
    }

    private static void RenderBoards(
        W.Body body,
        IReadOnlyList<ProgramBoardSectionDto> boards,
        WordTheme theme)
    {
        body.Append(CreateHeading("KONGRE KURULLARI / CONGRESS BOARDS", Heading1StyleId, theme));

        foreach (ProgramBoardSectionDto board in boards
                     .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                     .ThenBy(x => x.Name))
        {
            body.Append(CreateParagraph(
                board.Name,
                Heading2StyleId,
                W.JustificationValues.Left,
                before: 150,
                after: 80,
                color: theme.Primary,
                languageTag: "tr-TR",
                keepNext: true));

            W.Table table = CreateTable(new[] { 4000, 6000 }, borderless: true);
            foreach (ProgramBoardMemberPdfDto member in board.Members
                         .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                         .ThenBy(x => x.DisplayName))
            {
                table.Append(CreateTableRow(new[]
                {
                    CreateBoardCell(member.DisplayName, 4000, theme, bold: true),
                    CreateBoardCell(member.Institution, 6000, theme, bold: false)
                }));
            }

            body.Append(table);
            body.Append(CreateParagraph(string.Empty, after: 100));
        }
    }

    private static W.TableCell CreateBoardCell(
        string? text,
        int width,
        WordTheme theme,
        bool bold)
    {
        W.TableCell cell = new();
        W.TableCellProperties properties = CreateCellProperties(width, null, borderless: false);
        properties.Append(new W.TableCellBorders(
            new W.BottomBorder
            {
                Val = W.BorderValues.Single,
                Color = theme.Border,
                Size = 4U
            }));
        cell.Append(properties);
        cell.Append(CreateParagraph(
            text ?? string.Empty,
            justification: W.JustificationValues.Left,
            before: 55,
            after: 55,
            fontSizeHalfPoints: 16,
            bold: bold,
            color: bold ? "2D323C" : theme.Muted,
            languageTag: "tr-TR"));
        return cell;
    }

    private static void RenderContents(
        W.Body body,
        AbstractBookDocumentModel model,
        WordTheme theme)
    {
        body.Append(CreateParagraph(
            "ÖZET KİTABI / ABSTRACT BOOK",
            SmallMutedStyleId,
            W.JustificationValues.Center,
            after: 30,
            languageTag: "tr-TR"));
        body.Append(CreateParagraph(
            "İÇİNDEKİLER / CONTENTS",
            BookTitleStyleId,
            W.JustificationValues.Center,
            after: 80,
            color: theme.Primary,
            languageTag: "tr-TR"));
        body.Append(CreateParagraph(
            $"{model.Entries.Count} bildiri / submissions",
            SmallMutedStyleId,
            W.JustificationValues.Center,
            after: 220,
            languageTag: "tr-TR"));
        body.Append(CreateRuleParagraph(theme));

        W.Table toc = CreateTable(new[] { 700, 7900, 1400 }, borderless: true);
        int sequence = 1;
        foreach (AbstractBookEntryDto entry in model.Entries)
        {
            string title = model.Options.IncludeTurkishContent
                ? FirstNonEmpty(entry.TurkishTitle, entry.EnglishTitle, entry.SubmissionNumber)
                : FirstNonEmpty(entry.EnglishTitle, entry.TurkishTitle, entry.SubmissionNumber);

            toc.Append(CreateTableRow(new[]
            {
                CreateTocCell(sequence.ToString("00", CultureInfo.InvariantCulture), 700, theme, true),
                CreateTocCell(title, 7900, theme, false),
                CreateTocPageCell(BuildAbstractBookmarkName(entry.Id), 1400, theme)
            }));
            sequence++;
        }

        body.Append(toc);
    }

    private static W.TableCell CreateTocCell(
        string text,
        int width,
        WordTheme theme,
        bool compact)
    {
        W.TableCell cell = new();
        W.TableCellProperties properties = CreateCellProperties(width, null, borderless: false);
        properties.Append(new W.TableCellBorders(
            new W.BottomBorder
            {
                Val = W.BorderValues.Single,
                Color = theme.Border,
                Size = 4U
            }));
        cell.Append(properties);
        cell.Append(CreateParagraph(
            text,
            justification: compact ? W.JustificationValues.Center : W.JustificationValues.Left,
            before: 55,
            after: 55,
            fontSizeHalfPoints: compact ? 14 : 16,
            bold: !compact,
            color: compact ? theme.Muted : "2D323C",
            languageTag: "tr-TR"));
        return cell;
    }

    private static void RenderFullTextContents(
        W.Body body,
        AbstractBookDocumentModel model,
        IReadOnlyList<FullTextBookDocumentDto> fullTextDocuments,
        WordTheme theme)
    {
        _ = fullTextDocuments;

        body.Append(CreateParagraph(
            "İÇİNDEKİLER / CONTENTS",
            BookTitleStyleId,
            W.JustificationValues.Center,
            after: 80,
            color: theme.Primary,
            languageTag: "tr-TR"));
        body.Append(CreateRuleParagraph(theme));

        W.Table toc = CreateTable(new[] { 700, 7900, 1400 }, borderless: true);
        int sequence = 1;
        foreach (AbstractBookEntryDto entry in model.Entries)
        {
            string title = model.Options.IncludeTurkishContent
                ? FirstNonEmpty(entry.TurkishTitle, entry.EnglishTitle, entry.SubmissionNumber)
                : FirstNonEmpty(entry.EnglishTitle, entry.TurkishTitle, entry.SubmissionNumber);

            toc.Append(CreateTableRow(new[]
            {
                CreateFullTextTocCell(
                    sequence.ToString("00", CultureInfo.InvariantCulture),
                    700,
                    theme,
                    compact: true),
                CreateFullTextTocCell(title, 7900, theme, compact: false),
                CreateTocPageCell(BuildFullTextBookmarkName(entry.Id), 1400, theme)
            }));
            sequence++;
        }

        body.Append(toc);
    }

    private static W.TableCell CreateFullTextTocCell(
        string text,
        int width,
        WordTheme theme,
        bool compact,
        bool header = false,
        bool section = false)
    {
        W.TableCell cell = new();
        string? background = header || section ? theme.SoftBackground : null;
        W.TableCellProperties properties = CreateCellProperties(width, background, borderless: false);
        properties.Append(new W.TableCellBorders(
            new W.BottomBorder
            {
                Val = W.BorderValues.Single,
                Color = theme.Border,
                Size = 4U
            }));
        cell.Append(properties);
        cell.Append(CreateParagraph(
            text,
            justification: compact ? W.JustificationValues.Center : W.JustificationValues.Left,
            before: header || section ? 70 : 55,
            after: header || section ? 70 : 55,
            fontSizeHalfPoints: header ? 13 : section ? 15 : compact ? 14 : 16,
            bold: header || section || !compact,
            color: header || section ? theme.Primary : compact ? theme.Muted : "2D323C",
            languageTag: "tr-TR"));
        return cell;
    }

    private static W.TableCell CreateTocPageCell(
        string bookmarkName,
        int width,
        WordTheme theme)
    {
        W.TableCell cell = new();
        W.TableCellProperties properties = CreateCellProperties(width, null, borderless: false);
        properties.Append(new W.TableCellBorders(
            new W.BottomBorder
            {
                Val = W.BorderValues.Single,
                Color = theme.Border,
                Size = 4U
            }));
        cell.Append(properties);

        W.SimpleField pageReference = new(
            new W.Run(
                CreateRunProperties(fontSizeHalfPoints: 14, color: theme.Muted),
                new W.Text("0")))
        {
            Instruction = new O.StringValue($" PAGEREF {bookmarkName} \\h "),
            Dirty = new O.OnOffValue(true)
        };

        cell.Append(new W.Paragraph(
            new W.ParagraphProperties(
                new W.Justification { Val = W.JustificationValues.Center },
                new W.SpacingBetweenLines { Before = "55", After = "55" }),
            pageReference));
        return cell;
    }

    private static void RenderEntries(
        W.Body body,
        P.MainDocumentPart mainPart,
        AbstractBookDocumentModel model,
        WordTheme theme,
        bool includeBookmarks)
    {
        for (int index = 0; index < model.Entries.Count; index++)
        {
            AbstractBookEntryDto entry = model.Entries[index];
            if (index > 0 && model.Options.StartEachSubmissionOnNewPage)
                AddPageBreakBefore(body);

            if (includeBookmarks)
            {
                body.Append(CreateBookmarkAnchor(
                    BuildAbstractBookmarkName(entry.Id),
                    100000 + index));
            }

            body.Append(CreateSubmissionMetadataTable(entry, theme));
            body.Append(CreateSubmissionOrcidParagraph(
                entry.Authors,
                model.Options.IncludeOrcid,
                theme));

            bool hasTurkishHeading = model.Options.IncludeTurkishContent
                                     && !string.IsNullOrWhiteSpace(entry.TurkishTitle);
            if (hasTurkishHeading)
            {
                body.Append(CreateParagraph(
                    entry.TurkishTitle,
                    Heading2StyleId,
                    W.JustificationValues.Center,
                    before: 150,
                    after: 100,
                    color: theme.Primary,
                    languageTag: "tr-TR",
                    keepNext: true,
                    keepLines: true));
            }

            RenderAuthors(body, entry.Authors, model.Options, theme);

            bool renderedContent = false;
            if (model.Options.IncludeTurkishContent && !string.IsNullOrWhiteSpace(entry.TurkishAbstract))
            {
                RenderContentSection(
                    body,
                    "ÖZET",
                    entry.TurkishAbstract,
                    "Anahtar Kelimeler",
                    entry.TurkishKeywords,
                    theme,
                    "tr-TR");
                renderedContent = true;
            }

            if (model.Options.IncludeEnglishContent && !string.IsNullOrWhiteSpace(entry.EnglishTitle))
            {
                body.Append(CreateParagraph(
                    entry.EnglishTitle,
                    hasTurkishHeading ? null : Heading2StyleId,
                    W.JustificationValues.Center,
                    before: renderedContent ? 90 : 140,
                    after: 80,
                    fontSizeHalfPoints: hasTurkishHeading ? 21 : 24,
                    italic: true,
                    color: hasTurkishHeading ? theme.Muted : theme.Primary,
                    languageTag: "en-US",
                    keepNext: true,
                    keepLines: true));
            }

            if (model.Options.IncludeEnglishContent && !string.IsNullOrWhiteSpace(entry.EnglishAbstract))
            {
                RenderContentSection(
                    body,
                    "ABSTRACT",
                    entry.EnglishAbstract,
                    "Keywords",
                    entry.EnglishKeywords,
                    theme,
                    "en-US");
                renderedContent = true;
            }

            if (!renderedContent)
            {
                body.Append(CreateParagraph(
                    "Bu bildiri için seçilen dilde özet içeriği bulunamadı. / No abstract content is available in the selected language.",
                    SmallMutedStyleId,
                    W.JustificationValues.Center,
                    before: 220,
                    languageTag: "tr-TR"));
            }

            if (index + 1 < model.Entries.Count && !model.Options.StartEachSubmissionOnNewPage)
            {
                body.Append(CreateRuleParagraph(theme));
                body.Append(CreateParagraph(string.Empty, after: 140));
            }
        }
    }

    private static void RenderFullTextEntries(
        W.Body body,
        P.MainDocumentPart mainPart,
        AbstractBookDocumentModel model,
        IReadOnlyList<FullTextBookDocumentDto> documents,
        WordTheme theme)
    {
        IReadOnlyDictionary<Guid, FullTextBookDocumentDto> documentsBySubmissionId = documents
            .GroupBy(document => document.SubmissionId)
            .ToDictionary(group => group.Key, group => group.First());

        for (int index = 0; index < model.Entries.Count; index++)
        {
            AbstractBookEntryDto entry = model.Entries[index];
            if (!documentsBySubmissionId.TryGetValue(entry.Id, out FullTextBookDocumentDto document))
                continue;

            if (index > 0)
                AddPageBreakBefore(body);

            body.Append(CreateBookmarkAnchor(
                BuildFullTextBookmarkName(entry.Id),
                200000 + index));

            RenderFullTextEntryHeader(
                body,
                entry,
                model.Options,
                theme);

            AppendFullTextDocument(body, mainPart, document);
        }
    }

    private static void RenderFullTextEntryHeader(
        W.Body body,
        AbstractBookEntryDto entry,
        AbstractBookOptionsDto options,
        WordTheme theme)
    {
        body.Append(CreateSubmissionMetadataTable(entry, theme));
        body.Append(CreateSubmissionOrcidParagraph(
            entry.Authors,
            options.IncludeOrcid,
            theme));

        bool hasTurkishHeading = options.IncludeTurkishContent
                                 && !string.IsNullOrWhiteSpace(entry.TurkishTitle);
        if (hasTurkishHeading)
        {
            body.Append(CreateParagraph(
                entry.TurkishTitle,
                Heading2StyleId,
                W.JustificationValues.Center,
                before: 150,
                after: 80,
                color: theme.Primary,
                languageTag: "tr-TR",
                keepNext: true,
                keepLines: true));
        }

        if (options.IncludeEnglishContent && !string.IsNullOrWhiteSpace(entry.EnglishTitle))
        {
            body.Append(CreateParagraph(
                entry.EnglishTitle,
                hasTurkishHeading ? null : Heading2StyleId,
                W.JustificationValues.Center,
                before: hasTurkishHeading ? 20 : 140,
                after: 80,
                fontSizeHalfPoints: hasTurkishHeading ? 21 : 24,
                italic: hasTurkishHeading,
                color: hasTurkishHeading ? theme.Muted : theme.Primary,
                languageTag: "en-US",
                keepNext: true,
                keepLines: true));
        }

        RenderAuthors(body, entry.Authors, options, theme);
    }

    private static void AppendFullTextDocument(
        W.Body body,
        P.MainDocumentPart mainPart,
        FullTextBookDocumentDto document)
    {
        P.AlternativeFormatImportPart importPart = mainPart.AddAlternativeFormatImportPart(
            P.AlternativeFormatImportPartType.WordprocessingML);
        using MemoryStream source = new(document.Content, writable: false);
        importPart.FeedData(source);

        body.Append(new W.AltChunk
        {
            Id = new O.StringValue(mainPart.GetIdOfPart(importPart))
        });
    }

    private static void RenderProceedingsDividerPage(
        W.Body body,
        WordTheme theme)
    {
        body.Append(CreateParagraph(
            "TAM METİNLER",
            justification: W.JustificationValues.Center,
            before: 3000,
            after: 220,
            fontSizeHalfPoints: 60,
            color: theme.Primary,
            languageTag: "tr-TR",
            keepNext: true,
            keepLines: true));

        body.Append(CreateParagraph(
            "PROCEEDINGS",
            justification: W.JustificationValues.Center,
            before: 80,
            after: 0,
            fontSizeHalfPoints: 52,
            color: theme.Primary,
            languageTag: "en-US",
            keepNext: true,
            keepLines: true));
    }

    private static W.Paragraph CreateBookmarkAnchor(string bookmarkName, int bookmarkId)
    {
        string normalizedId = bookmarkId.ToString(CultureInfo.InvariantCulture);
        return new W.Paragraph(
            new W.ParagraphProperties(
                new W.SpacingBetweenLines
                {
                    Before = "0",
                    After = "0",
                    Line = "1",
                    LineRule = W.LineSpacingRuleValues.Exact
                }),
            new W.BookmarkStart
            {
                Name = new O.StringValue(bookmarkName),
                Id = new O.StringValue(normalizedId)
            },
            new W.Run(
                CreateRunProperties(fontSizeHalfPoints: 2),
                new W.Text(string.Empty)),
            new W.BookmarkEnd
            {
                Id = new O.StringValue(normalizedId)
            });
    }

    private static string BuildAbstractBookmarkName(Guid submissionId)
        => $"a_{submissionId:N}";

    private static string BuildFullTextBookmarkName(Guid submissionId)
        => $"f_{submissionId:N}";

    private static W.Table CreateDocumentHeaderTable(
        P.HeaderPart headerPart,
        AbstractBookDocumentModel model,
        WordTheme theme,
        string? culture)
    {
        W.Table table = CreateTable(new[] { 1400, 7200, 1400 }, borderless: true);

        W.TableCell titleCell = new();
        titleCell.Append(CreateCellProperties(7200, null, borderless: true));
        titleCell.Append(CreateParagraph(
            FirstNonEmpty(model.CongressEnglishName, model.CongressName),
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: 15,
            bold: true,
            color: theme.Primary,
            after: 0,
            languageTag: "en-US",
            keepLines: true));

        string dateText = FormatHeaderDateRange(model.StartDate, model.EndDate, "en-US");
        if (!string.IsNullOrWhiteSpace(dateText))
        {
            titleCell.Append(CreateParagraph(
                dateText,
                justification: W.JustificationValues.Center,
                fontSizeHalfPoints: 13,
                color: theme.Muted,
                after: 0,
                keepLines: true));
        }

        string location = BuildHeaderLocation(model.City, model.Venue);
        if (!string.IsNullOrWhiteSpace(location))
        {
            titleCell.Append(CreateParagraph(
                location,
                justification: W.JustificationValues.Center,
                fontSizeHalfPoints: 13,
                color: theme.Muted,
                after: 0,
                keepLines: true));
        }

        table.Append(CreateTableRow(new[]
        {
            CreateDocumentHeaderLogoCell(
                headerPart,
                model.Options.HeaderLogoBytes,
                model.Options.HeaderLogoContentType,
                1400,
                W.JustificationValues.Left),
            titleCell,
            CreateDocumentHeaderLogoCell(
                headerPart,
                model.Options.HeaderLogoBytes,
                model.Options.HeaderLogoContentType,
                1400,
                W.JustificationValues.Right)
        }));

        return table;
    }

    private static W.TableCell CreateDocumentHeaderLogoCell(
        P.HeaderPart headerPart,
        byte[]? logoBytes,
        string? contentType,
        int width,
        W.JustificationValues justification)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, null, borderless: true));

        W.Paragraph paragraph = new(
            new W.ParagraphProperties(
                new W.Justification { Val = justification },
                new W.SpacingBetweenLines { Before = "0", After = "0" }));

        if (logoBytes is { Length: > 0 })
        {
            (long logoWidth, long logoHeight) = ResolveHeaderLogoSize(logoBytes);
            paragraph.Append(new W.Run(CreateHeaderImageDrawing(
                headerPart,
                logoBytes,
                contentType,
                logoWidth,
                logoHeight)));
        }

        cell.Append(paragraph);
        return cell;
    }

    private static (long Width, long Height) ResolveHeaderLogoSize(byte[] logoBytes)
    {
        const long maxWidth = 520000L;
        const long maxHeight = 420000L;

        if (!TryReadImageDimensions(logoBytes, out int pixelWidth, out int pixelHeight)
            || pixelWidth <= 0
            || pixelHeight <= 0)
        {
            return (maxHeight, maxHeight);
        }

        double scale = Math.Min(
            maxWidth / (double)pixelWidth,
            maxHeight / (double)pixelHeight);

        return (
            Math.Max(1L, (long)Math.Round(pixelWidth * scale)),
            Math.Max(1L, (long)Math.Round(pixelHeight * scale)));
    }

    private static W.Table CreateSubmissionMetadataTable(AbstractBookEntryDto entry, WordTheme theme)
    {
        const int submissionNumberWidth = 3600;
        const int submissionTypeWidth = 2400;
        const int topicWidth = 4000;

        W.Table table = CreateTable(
            new[] { submissionNumberWidth, submissionTypeWidth, topicWidth },
            borderless: true);

        table.Append(CreateTableRow(new[]
        {
            CreateMetaCell(
                "BİLDİRİ NO / SUBMISSION NO",
                entry.SubmissionNumber,
                submissionNumberWidth,
                theme,
                W.JustificationValues.Left),
            CreateMetaCell(
                "TÜR / TYPE",
                entry.SubmissionTypeName,
                submissionTypeWidth,
                theme,
                W.JustificationValues.Center),
            CreateMetaCell(
                "KONU / TOPIC",
                entry.TopicName,
                topicWidth,
                theme,
                W.JustificationValues.Right)
        }));

        return table;
    }

    private static W.Paragraph CreateSubmissionOrcidParagraph(
        IReadOnlyList<AbstractBookAuthorDto> authors,
        bool includeOrcid,
        WordTheme theme)
    {
        string joinedOrcids = includeOrcid
            ? string.Join(
                " | ",
                authors
                    .Select(author => NormalizeOrcidForDisplay(author.Orcid))
                    .Where(orcid => !string.IsNullOrWhiteSpace(orcid))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
            : string.Empty;

        string text = string.IsNullOrWhiteSpace(joinedOrcids)
            ? "ORCID:"
            : $"ORCID: {joinedOrcids}";

        return CreateParagraph(
            text,
            justification: W.JustificationValues.Center,
            before: 80,
            after: 110,
            fontSizeHalfPoints: 19,
            bold: true,
            color: theme.Muted,
            languageTag: "en-US",
            keepNext: true,
            keepLines: true);
    }

    private static W.TableCell CreateMetaCell(
        string label,
        string value,
        int width,
        WordTheme theme,
        W.JustificationValues justification,
        bool showPlaceholderWhenEmpty = true)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, theme.SoftBackground, borderless: true));
        cell.Append(CreateParagraph(
            label,
            justification: justification,
            fontSizeHalfPoints: 13,
            bold: true,
            color: theme.Muted,
            after: 20));

        string displayValue = string.IsNullOrWhiteSpace(value)
            ? showPlaceholderWhenEmpty ? "-" : string.Empty
            : value;

        cell.Append(CreateParagraph(
            displayValue,
            justification: justification,
            fontSizeHalfPoints: 16,
            color: "232A36",
            after: 0));
        return cell;
    }

    private static void RenderAuthors(
        W.Body body,
        IReadOnlyList<AbstractBookAuthorDto> authors,
        AbstractBookOptionsDto options,
        WordTheme theme)
    {
        if (authors.Count == 0)
            return;

        IReadOnlyDictionary<string, int> institutionNumbers = options.IncludeInstitutions
            ? BuildInstitutionNumbers(authors)
            : new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

        body.Append(CreateAuthorsLineParagraph(
            authors,
            institutionNumbers,
            options.IncludeCorrespondingAuthor,
            theme));

        if (options.IncludeInstitutions)
        {
            foreach ((string institution, int number) in institutionNumbers
                         .OrderBy(item => item.Value)
                         .Select(item => (item.Key, item.Value)))
            {
                body.Append(CreateInstitutionParagraph(number, institution, theme));
            }
        }

        if (options.IncludeCorrespondingAuthor)
        {
            IReadOnlyList<AbstractBookAuthorDto> correspondingAuthors = authors
                .Where(author => author.IsCorrespondingAuthor)
                .ToList();

            foreach (AbstractBookAuthorDto author in correspondingAuthors)
            {
                body.Append(CreateCorrespondingAuthorParagraph(author, theme));

                if (!string.IsNullOrWhiteSpace(author.Email))
                {
                    body.Append(CreateParagraph(
                        author.Email.Trim(),
                        justification: W.JustificationValues.Center,
                        before: 0,
                        after: 20,
                        fontSizeHalfPoints: 14,
                        color: theme.Muted,
                        languageTag: "en-US",
                        keepLines: true));
                }
            }
        }

        body.Append(CreateParagraph(string.Empty, after: 90));
    }

    private static IReadOnlyDictionary<string, int> BuildInstitutionNumbers(
        IReadOnlyList<AbstractBookAuthorDto> authors)
    {
        Dictionary<string, int> numbers = new(StringComparer.CurrentCultureIgnoreCase);

        foreach (AbstractBookAuthorDto author in authors)
        {
            string institution = author.Institution?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(institution) || numbers.ContainsKey(institution))
                continue;

            numbers[institution] = numbers.Count + 1;
        }

        return numbers;
    }

    private static W.Paragraph CreateAuthorsLineParagraph(
        IReadOnlyList<AbstractBookAuthorDto> authors,
        IReadOnlyDictionary<string, int> institutionNumbers,
        bool includeCorrespondingAuthor,
        WordTheme theme)
    {
        W.Paragraph paragraph = new(
            new W.ParagraphProperties(
                new W.Justification { Val = W.JustificationValues.Center },
                new W.SpacingBetweenLines
                {
                    Before = "70",
                    After = "35",
                    Line = "240",
                    LineRule = W.LineSpacingRuleValues.Auto
                },
                new W.KeepLines(),
                new W.WidowControl()));

        for (int index = 0; index < authors.Count; index++)
        {
            AbstractBookAuthorDto author = authors[index];

            if (index > 0)
            {
                paragraph.Append(new W.Run(
                    CreateRunProperties(fontSizeHalfPoints: 16, color: "232A36"),
                    new W.Text(", ") { Space = O.SpaceProcessingModeValues.Preserve }));
            }

            paragraph.Append(new W.Run(
                CreateRunProperties(
                    bold: true,
                    fontSizeHalfPoints: 17,
                    color: "232A36"),
                new W.Text(author.DisplayName?.Trim() ?? string.Empty)
                {
                    Space = O.SpaceProcessingModeValues.Preserve
                }));

            if (includeCorrespondingAuthor && author.IsCorrespondingAuthor)
            {
                paragraph.Append(new W.Run(
                    CreateRunProperties(
                        bold: true,
                        fontSizeHalfPoints: 17,
                        color: "232A36"),
                    new W.Text("*")));
            }

            string institution = author.Institution?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(institution)
                && institutionNumbers.TryGetValue(institution, out int number))
            {
                paragraph.Append(new W.Run(
                    CreateRunProperties(
                        bold: true,
                        fontSizeHalfPoints: 12,
                        color: theme.Muted,
                        superscript: true),
                    new W.Text(number.ToString(CultureInfo.InvariantCulture))));
            }
        }

        return paragraph;
    }

    private static W.Paragraph CreateInstitutionParagraph(
        int number,
        string institution,
        WordTheme theme)
    {
        W.Paragraph paragraph = new(
            new W.ParagraphProperties(
                new W.Justification { Val = W.JustificationValues.Center },
                new W.SpacingBetweenLines
                {
                    Before = "0",
                    After = "15",
                    Line = "220",
                    LineRule = W.LineSpacingRuleValues.Auto
                },
                new W.KeepLines(),
                new W.WidowControl()));

        paragraph.Append(new W.Run(
            CreateRunProperties(
                fontSizeHalfPoints: 11,
                color: theme.Muted,
                superscript: true),
            new W.Text(number.ToString(CultureInfo.InvariantCulture))));

        paragraph.Append(new W.Run(
            CreateRunProperties(
                fontSizeHalfPoints: 14,
                color: theme.Muted),
            new W.Text(" " + institution)
            {
                Space = O.SpaceProcessingModeValues.Preserve
            }));

        return paragraph;
    }

    private static W.Paragraph CreateCorrespondingAuthorParagraph(
        AbstractBookAuthorDto author,
        WordTheme theme)
    {
        string authorName = FirstNonEmpty(author.PlainName, author.DisplayName);

        W.Paragraph paragraph = new(
            new W.ParagraphProperties(
                new W.Justification { Val = W.JustificationValues.Center },
                new W.SpacingBetweenLines
                {
                    Before = "35",
                    After = "10",
                    Line = "220",
                    LineRule = W.LineSpacingRuleValues.Auto
                },
                new W.KeepLines(),
                new W.WidowControl()));

        paragraph.Append(new W.Run(
            CreateRunProperties(
                fontSizeHalfPoints: 14,
                color: theme.Muted,
                languageTag: "en-US"),
            new W.Text($"* Corresponding Author: {authorName}")
            {
                Space = O.SpaceProcessingModeValues.Preserve
            }));

        return paragraph;
    }

    private static void RenderContentSection(
        W.Body body,
        string heading,
        string content,
        string keywordLabel,
        string keywords,
        WordTheme theme,
        string languageTag)
    {
        W.Table headingTable = CreateTable(new[] { 10000 }, borderless: true);
        W.TableCell headingCell = new();
        W.TableCellProperties headingCellProperties = CreateCellProperties(10000, theme.Primary, borderless: true);
        headingCell.Append(headingCellProperties);
        headingCell.Append(CreateParagraph(
            heading,
            justification: W.JustificationValues.Left,
            after: 0,
            fontSizeHalfPoints: 19,
            bold: true,
            color: "FFFFFF",
            languageTag: languageTag,
            keepNext: true));
        headingTable.Append(CreateTableRow(new[] { headingCell }));
        body.Append(headingTable);

        IReadOnlyList<string> paragraphs = NormalizeAbstractParagraphs(content, heading);
        for (int index = 0; index < paragraphs.Count; index++)
        {
            body.Append(CreateStructuredAbstractParagraph(
                paragraphs[index],
                languageTag,
                after: index == paragraphs.Count - 1 ? 80 : 90));
        }

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            W.Paragraph keywordParagraph = new(
                new W.ParagraphProperties(
                    new W.Justification { Val = W.JustificationValues.Left },
                    new W.SpacingBetweenLines { Before = "70", After = "80" },
                    new W.KeepLines()));
            keywordParagraph.Append(new W.Run(
                CreateRunProperties(
                    bold: true,
                    fontSizeHalfPoints: 17,
                    color: theme.Primary,
                    languageTag: languageTag),
                new W.Text(keywordLabel + ": ") { Space = O.SpaceProcessingModeValues.Preserve }));
            keywordParagraph.Append(new W.Run(
                CreateRunProperties(
                    fontSizeHalfPoints: 17,
                    color: "3C424D",
                    languageTag: languageTag),
                new W.Text(keywords) { Space = O.SpaceProcessingModeValues.Preserve }));
            body.Append(keywordParagraph);
        }
    }

    private static IReadOnlyList<string> NormalizeAbstractParagraphs(string content, string heading)
    {
        List<string> paragraphs = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        while (paragraphs.Count > 0 && IsDuplicateSectionHeading(paragraphs[0], heading))
            paragraphs.RemoveAt(0);

        return paragraphs;
    }

    private static bool IsDuplicateSectionHeading(string value, string heading)
    {
        string normalized = value.Trim().TrimEnd(':').Trim();
        return string.Equals(normalized, heading, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "Özet", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "Abstract", StringComparison.OrdinalIgnoreCase);
    }

    private static W.Paragraph CreateStructuredAbstractParagraph(
        string text,
        string languageTag,
        int after)
    {
        W.ParagraphProperties properties = new(
            new W.Justification { Val = W.JustificationValues.Both },
            new W.SpacingBetweenLines
            {
                Before = "0",
                After = after.ToString(CultureInfo.InvariantCulture),
                Line = "300",
                LineRule = W.LineSpacingRuleValues.Auto
            },
            new W.WidowControl());

        W.Paragraph paragraph = new(properties);
        (string? label, string remainder) = SplitStructuredLabel(text, languageTag);
        if (!string.IsNullOrWhiteSpace(label))
        {
            paragraph.Append(new W.Run(
                CreateRunProperties(
                    bold: true,
                    fontSizeHalfPoints: 18,
                    color: "20242B",
                    languageTag: languageTag),
                new W.Text(label + ": ") { Space = O.SpaceProcessingModeValues.Preserve }));
        }

        paragraph.Append(new W.Run(
            CreateRunProperties(
                fontSizeHalfPoints: 18,
                color: "20242B",
                languageTag: languageTag),
            new W.Text(remainder) { Space = O.SpaceProcessingModeValues.Preserve }));
        return paragraph;
    }

    private static (string? Label, string Remainder) SplitStructuredLabel(
        string text,
        string languageTag)
    {
        string[] labels = languageTag.StartsWith("tr", StringComparison.OrdinalIgnoreCase)
            ? new[]
            {
                "Giriş ve Amaç", "Amaç", "Giriş", "Gereç ve Yöntem", "Gereç-Yöntem",
                "Materyal ve Metot", "Yöntem", "Bulgular", "Tartışma", "Sonuç", "Sonuçlar"
            }
            : new[]
            {
                "Background and Objective", "Background", "Objective", "Aim", "Introduction",
                "Materials and Methods", "Material and Method", "Methods", "Method",
                "Results", "Discussion", "Conclusion", "Conclusions"
            };

        string candidate = text.Trim();
        foreach (string label in labels.OrderByDescending(x => x.Length))
        {
            if (!candidate.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                continue;

            string remainder = candidate[label.Length..].TrimStart();
            if (remainder.StartsWith(":", StringComparison.Ordinal))
                remainder = remainder[1..].TrimStart();
            else if (remainder.StartsWith("-", StringComparison.Ordinal))
                remainder = remainder[1..].TrimStart();
            else if (remainder.Length > 0 && char.IsLetterOrDigit(remainder[0]))
                continue;

            return (label, remainder);
        }

        return (null, candidate);
    }

    private static void AddStyles(P.MainDocumentPart mainPart, WordTheme theme)
    {
        P.StyleDefinitionsPart stylesPart = mainPart.AddNewPart<P.StyleDefinitionsPart>();
        W.Styles styles = new();
        styles.Append(CreateParagraphStyle("Normal", "Normal", 18, "20242B", isDefault: true));
        styles.Append(CreateParagraphStyle(BookTitleStyleId, "Book Title", 40, theme.Primary, bold: true));
        styles.Append(CreateParagraphStyle(BookSubtitleStyleId, "Book Subtitle", 28, theme.Primary, bold: true));
        styles.Append(CreateParagraphStyle(SmallMutedStyleId, "Small Muted", 15, theme.Muted));
        styles.Append(CreateParagraphStyle(Heading1StyleId, "heading 1", 34, theme.Primary, bold: true, outlineLevel: 0));
        styles.Append(CreateParagraphStyle(Heading2StyleId, "heading 2", 28, theme.Primary, bold: true, outlineLevel: 1));
        stylesPart.Styles = styles;
        stylesPart.Styles.Save();
    }

    private static W.Style CreateParagraphStyle(
        string styleId,
        string styleName,
        int fontSizeHalfPoints,
        string color,
        bool bold = false,
        bool isDefault = false,
        int? outlineLevel = null)
    {
        W.Style style = new()
        {
            Type = new O.EnumValue<W.StyleValues>(W.StyleValues.Paragraph),
            StyleId = new O.StringValue(styleId),
            Default = new O.OnOffValue(isDefault)
        };
        style.Append(new W.StyleName { Val = new O.StringValue(styleName) });
        if (!string.Equals(styleId, "Normal", StringComparison.Ordinal))
        {
            style.Append(new W.BasedOn { Val = new O.StringValue("Normal") });
            style.Append(new W.NextParagraphStyle { Val = new O.StringValue("Normal") });
        }

        W.StyleParagraphProperties paragraphProperties = new(
            new W.SpacingBetweenLines
            {
                After = new O.StringValue("80"),
                Line = new O.StringValue("240"),
                LineRule = new O.EnumValue<W.LineSpacingRuleValues>(W.LineSpacingRuleValues.Auto)
            });
        if (outlineLevel.HasValue)
        {
            paragraphProperties.Append(new W.KeepNext());
            paragraphProperties.Append(new W.OutlineLevel { Val = new O.Int32Value(outlineLevel.Value) });
        }
        style.Append(paragraphProperties);

        W.StyleRunProperties runProperties = new(
            CreateRunFonts(),
            new W.FontSize { Val = new O.StringValue(fontSizeHalfPoints.ToString(CultureInfo.InvariantCulture)) },
            new W.FontSizeComplexScript { Val = new O.StringValue(fontSizeHalfPoints.ToString(CultureInfo.InvariantCulture)) },
            new W.Color { Val = new O.StringValue(color) });
        if (bold)
            runProperties.Append(new W.Bold());
        style.Append(runProperties);
        return style;
    }

    private static void AddSettings(
        P.MainDocumentPart mainPart,
        bool updateFieldsOnOpen)
    {
        P.DocumentSettingsPart settingsPart = mainPart.AddNewPart<P.DocumentSettingsPart>();
        W.Settings settings = new();

        if (updateFieldsOnOpen)
            settings.Append(new W.UpdateFieldsOnOpen { Val = new O.OnOffValue(true) });

        settings.Append(new W.Compatibility(
            new W.CompatibilitySetting
            {
                Name = new O.EnumValue<W.CompatSettingNameValues>(W.CompatSettingNameValues.CompatibilityMode),
                Uri = new O.StringValue("http://schemas.microsoft.com/office/word"),
                Val = new O.StringValue("15")
            }));

        settingsPart.Settings = settings;
        settingsPart.Settings.Save();
    }

    private static string AddHeader(
        P.MainDocumentPart mainPart,
        AbstractBookDocumentModel model,
        WordTheme theme,
        string? culture)
    {
        P.HeaderPart headerPart = mainPart.AddNewPart<P.HeaderPart>();
        headerPart.Header = new W.Header(CreateDocumentHeaderTable(headerPart, model, theme, culture));
        headerPart.Header.Save();
        return mainPart.GetIdOfPart(headerPart);
    }

    private static string AddFooter(P.MainDocumentPart mainPart)
    {
        P.FooterPart footerPart = mainPart.AddNewPart<P.FooterPart>();
        W.SimpleField pageField = new(new W.Run(
            CreateRunProperties(fontSizeHalfPoints: 14, color: "808080"),
            new W.Text("1")))
        {
            Instruction = new O.StringValue("PAGE")
        };

        footerPart.Footer = new W.Footer(
            new W.Paragraph(
                new W.ParagraphProperties(
                    new W.Justification
                    {
                        Val = new O.EnumValue<W.JustificationValues>(W.JustificationValues.Center)
                    }),
                new W.Run(CreateRunProperties(fontSizeHalfPoints: 14, color: "808080"), new W.Text("- ")),
                pageField,
                new W.Run(CreateRunProperties(fontSizeHalfPoints: 14, color: "808080"), new W.Text(" -"))));
        footerPart.Footer.Save();
        return mainPart.GetIdOfPart(footerPart);
    }

    private static W.SectionProperties CreateSectionProperties(
        string footerRelationshipId,
        string headerRelationshipId)
    {
        W.SectionProperties section = new();
        section.Append(new W.HeaderReference
        {
            Type = new O.EnumValue<W.HeaderFooterValues>(W.HeaderFooterValues.Default),
            Id = new O.StringValue(headerRelationshipId)
        });
        section.Append(new W.FooterReference
        {
            Type = new O.EnumValue<W.HeaderFooterValues>(W.HeaderFooterValues.Default),
            Id = new O.StringValue(footerRelationshipId)
        });
        section.Append(new W.PageSize
        {
            Width = new O.UInt32Value(11906U),
            Height = new O.UInt32Value(16838U)
        });
        section.Append(new W.PageMargin
        {
            Top = new O.Int32Value(1260),
            Right = new O.UInt32Value(920U),
            Bottom = new O.Int32Value(820),
            Left = new O.UInt32Value(920U),
            Header = new O.UInt32Value(220U),
            Footer = new O.UInt32Value(360U),
            Gutter = new O.UInt32Value(0U)
        });
        section.Append(new W.PageNumberType { Start = new O.Int32Value(1) });
        return section;
    }

    private static W.SectionProperties CreateCoverSectionProperties()
    {
        W.SectionProperties section = new();
        section.Append(new W.PageSize
        {
            Width = new O.UInt32Value(11906U),
            Height = new O.UInt32Value(16838U)
        });
        section.Append(new W.PageMargin
        {
            Top = new O.Int32Value(0),
            Right = new O.UInt32Value(0U),
            Bottom = new O.Int32Value(0),
            Left = new O.UInt32Value(0U),
            Header = new O.UInt32Value(0U),
            Footer = new O.UInt32Value(0U),
            Gutter = new O.UInt32Value(0U)
        });
        section.Append(new W.Columns { Space = new O.StringValue("708") });
        return section;
    }

    private static void ApplySectionPropertiesToLastParagraph(
        W.Body body,
        W.SectionProperties sectionProperties)
    {
        W.Paragraph? paragraph = body.Elements<W.Paragraph>().LastOrDefault();
        if (paragraph is null)
        {
            body.Append(new W.Paragraph(new W.ParagraphProperties(sectionProperties)));
            return;
        }

        W.ParagraphProperties properties = paragraph.GetFirstChild<W.ParagraphProperties>()
            ?? paragraph.PrependChild(new W.ParagraphProperties());

        properties.RemoveAllChildren<W.SectionProperties>();
        properties.Append(sectionProperties);
    }

    private static void AddPageBreakBefore(W.Body body)
    {
        body.Append(new W.Paragraph(
            new W.ParagraphProperties(
                new W.PageBreakBefore(),
                new W.KeepNext(),
                new W.SpacingBetweenLines
                {
                    Before = "0",
                    After = "0",
                    Line = "1",
                    LineRule = W.LineSpacingRuleValues.Exact
                }),
            new W.Run(
                CreateRunProperties(fontSizeHalfPoints: 2),
                new W.Text(string.Empty))));
    }

    private static W.Paragraph CreateHeading(string text, string styleId, WordTheme theme)
        => CreateParagraph(text, styleId, W.JustificationValues.Left, before: 120, after: 120, color: theme.Primary);

    private static W.Paragraph CreateRuleParagraph(WordTheme theme)
    {
        W.ParagraphProperties properties = new(
            new W.ParagraphBorders(
                new W.BottomBorder
                {
                    Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single),
                    Color = new O.StringValue(theme.Accent),
                    Size = new O.UInt32Value(14U),
                    Space = new O.UInt32Value(1U)
                }),
            new W.SpacingBetweenLines { After = new O.StringValue("80") });
        return new W.Paragraph(properties, new W.Run(new W.Text(string.Empty)));
    }

    private static W.Drawing CreateInlineCoverImageDrawing(
        P.MainDocumentPart mainPart,
        byte[] imageBytes,
        string? contentType,
        bool cropToFill)
    {
        const long pageWidthEmus = 7560310L;
        const long pageHeightEmus = 10692130L;

        P.PartTypeInfo imagePartType = ResolveImagePartType(imageBytes, contentType);
        P.ImagePart imagePart = mainPart.AddImagePart(imagePartType);
        using (MemoryStream stream = new(imageBytes))
            imagePart.FeedData(stream);

        string relationshipId = mainPart.GetIdOfPart(imagePart)
            ?? throw new InvalidOperationException("Kapak görseli Word paketine eklenemedi.");
        uint drawingId = unchecked((uint)Interlocked.Increment(ref _drawingIdSeed));

        A.Graphic graphic = new(
            new A.GraphicData(
                new PIC.Picture(
                    new PIC.NonVisualPictureProperties(
                        new PIC.NonVisualDrawingProperties
                        {
                            Id = drawingId,
                            Name = $"Abstract Book Cover {drawingId}"
                        },
                        new PIC.NonVisualPictureDrawingProperties()),
                    new PIC.BlipFill(
                        new A.Blip
                        {
                            Embed = relationshipId,
                            CompressionState = A.BlipCompressionValues.Print
                        },
                        CreateCoverSourceRectangle(imageBytes, cropToFill),
                        new A.Stretch(new A.FillRectangle())),
                    new PIC.ShapeProperties(
                        new A.Transform2D(
                            new A.Offset { X = 0L, Y = 0L },
                            new A.Extents { Cx = pageWidthEmus, Cy = pageHeightEmus }),
                        new A.PresetGeometry(new A.AdjustValueList())
                        {
                            Preset = A.ShapeTypeValues.Rectangle
                        })))
            {
                Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
            });

        DW.Inline inline = new(
            new DW.Extent { Cx = pageWidthEmus, Cy = pageHeightEmus },
            new DW.EffectExtent
            {
                LeftEdge = 0L,
                TopEdge = 0L,
                RightEdge = 0L,
                BottomEdge = 0L
            },
            new DW.DocProperties
            {
                Id = drawingId,
                Name = $"Abstract Book Cover {drawingId}"
            },
            new DW.NonVisualGraphicFrameDrawingProperties(
                new A.GraphicFrameLocks { NoChangeAspect = false }),
            graphic)
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U
        };

        return new W.Drawing(inline);
    }

    private static A.SourceRectangle CreateCoverSourceRectangle(
        byte[] imageBytes,
        bool cropToFill)
    {
        if (!cropToFill)
            return new A.SourceRectangle();

        if (!TryReadImageDimensions(imageBytes, out int width, out int height)
            || width <= 0
            || height <= 0)
        {
            return new A.SourceRectangle();
        }

        const double targetRatio = 11906d / 16838d;
        double sourceRatio = (double)width / height;
        int left = 0;
        int right = 0;
        int top = 0;
        int bottom = 0;

        if (sourceRatio > targetRatio)
        {
            double visibleRatio = targetRatio / sourceRatio;
            int crop = (int)Math.Round((1d - visibleRatio) * 50000d, MidpointRounding.AwayFromZero);
            left = crop;
            right = crop;
        }
        else if (sourceRatio < targetRatio)
        {
            double visibleRatio = sourceRatio / targetRatio;
            int crop = (int)Math.Round((1d - visibleRatio) * 50000d, MidpointRounding.AwayFromZero);
            top = crop;
            bottom = crop;
        }

        return new A.SourceRectangle
        {
            Left = new O.Int32Value(left),
            Right = new O.Int32Value(right),
            Top = new O.Int32Value(top),
            Bottom = new O.Int32Value(bottom)
        };
    }

    private static bool TryReadImageDimensions(
        byte[] bytes,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (bytes.Length >= 24
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47)
        {
            width = ReadBigEndianInt32(bytes, 16);
            height = ReadBigEndianInt32(bytes, 20);
            return width > 0 && height > 0;
        }

        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            return false;

        int offset = 2;
        while (offset + 8 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            while (offset < bytes.Length && bytes[offset] == 0xFF)
                offset++;
            if (offset >= bytes.Length)
                break;

            byte marker = bytes[offset++];
            if (marker is 0xD8 or 0xD9)
                continue;
            if (offset + 1 >= bytes.Length)
                break;

            int segmentLength = (bytes[offset] << 8) | bytes[offset + 1];
            if (segmentLength < 2 || offset + segmentLength > bytes.Length)
                break;

            bool isStartOfFrame = marker is 0xC0 or 0xC1 or 0xC2 or 0xC3
                or 0xC5 or 0xC6 or 0xC7
                or 0xC9 or 0xCA or 0xCB
                or 0xCD or 0xCE or 0xCF;
            if (isStartOfFrame && segmentLength >= 7)
            {
                height = (bytes[offset + 3] << 8) | bytes[offset + 4];
                width = (bytes[offset + 5] << 8) | bytes[offset + 6];
                return width > 0 && height > 0;
            }

            offset += segmentLength;
        }

        return false;
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
        => (bytes[offset] << 24)
           | (bytes[offset + 1] << 16)
           | (bytes[offset + 2] << 8)
           | bytes[offset + 3];

    private static W.Paragraph CreateImageParagraph(
        P.MainDocumentPart mainPart,
        byte[] imageBytes,
        string? contentType,
        long widthEmus,
        long heightEmus,
        W.JustificationValues justification,
        int before = 0,
        int after = 0)
    {
        W.Paragraph paragraph = new(
            new W.ParagraphProperties(
                new W.Justification { Val = justification },
                new W.SpacingBetweenLines
                {
                    Before = before.ToString(CultureInfo.InvariantCulture),
                    After = after.ToString(CultureInfo.InvariantCulture)
                }));

        paragraph.Append(new W.Run(CreateImageDrawing(mainPart, imageBytes, contentType, widthEmus, heightEmus)));
        return paragraph;
    }

    private static W.Drawing CreateHeaderImageDrawing(
        P.HeaderPart headerPart,
        byte[] imageBytes,
        string? contentType,
        long widthEmus,
        long heightEmus)
    {
        P.PartTypeInfo imagePartType = ResolveImagePartType(imageBytes, contentType);
        P.ImagePart imagePart = headerPart.AddImagePart(imagePartType);

        using MemoryStream stream = new(imageBytes);
        imagePart.FeedData(stream);

        string relationshipId = headerPart.GetIdOfPart(imagePart)!;
        return CreateInlineImageDrawing(relationshipId, widthEmus, heightEmus);
    }

    private static W.Drawing CreateImageDrawing(
        P.MainDocumentPart mainPart,
        byte[] imageBytes,
        string? contentType,
        long widthEmus,
        long heightEmus)
    {
        P.PartTypeInfo imagePartType = ResolveImagePartType(imageBytes, contentType);
        P.ImagePart imagePart = mainPart.AddImagePart(imagePartType);

        using MemoryStream stream = new(imageBytes);
        imagePart.FeedData(stream);

        string relationshipId = mainPart.GetIdOfPart(imagePart)!;
        return CreateInlineImageDrawing(relationshipId, widthEmus, heightEmus);
    }

    private static W.Drawing CreateInlineImageDrawing(
        string relationshipId,
        long widthEmus,
        long heightEmus)
    {
        uint drawingId = unchecked((uint)Interlocked.Increment(ref _drawingIdSeed));
        O.UInt32Value docPrId = new(drawingId);

        DW.Inline inline = new(
            new DW.Extent { Cx = widthEmus, Cy = heightEmus },
            new DW.EffectExtent
            {
                LeftEdge = 0L,
                TopEdge = 0L,
                RightEdge = 0L,
                BottomEdge = 0L
            },
            new DW.DocProperties { Id = docPrId, Name = $"Picture {drawingId}" },
            new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
            new A.Graphic(
                new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = drawingId, Name = $"Image {drawingId}" },
                            new PIC.NonVisualPictureDrawingProperties()),
                        new PIC.BlipFill(
                            new A.Blip { Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print },
                            new A.Stretch(new A.FillRectangle())),
                        new PIC.ShapeProperties(
                            new A.Transform2D(
                                new A.Offset { X = 0L, Y = 0L },
                                new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U
        };

        // wp:inline, w:r altında doğrudan bulunamaz. WordprocessingML şemasında
        // run -> w:drawing -> wp:inline zinciri zorunludur. Doğrudan wp:inline
        // eklemek LibreOffice tarafından tolere edilse de Microsoft Word paketi bozuk sayar.
        return new W.Drawing(inline);
    }

    private static P.PartTypeInfo ResolveImagePartType(
        byte[] imageBytes,
        string? contentType)
    {
        if (imageBytes.Length >= 8
            && imageBytes[0] == 0x89
            && imageBytes[1] == 0x50
            && imageBytes[2] == 0x4E
            && imageBytes[3] == 0x47
            && imageBytes[4] == 0x0D
            && imageBytes[5] == 0x0A
            && imageBytes[6] == 0x1A
            && imageBytes[7] == 0x0A)
        {
            return P.ImagePartType.Png;
        }

        if (imageBytes.Length >= 3
            && imageBytes[0] == 0xFF
            && imageBytes[1] == 0xD8
            && imageBytes[2] == 0xFF)
        {
            return P.ImagePartType.Jpeg;
        }

        return ResolveImagePartType(contentType);
    }

    private static P.PartTypeInfo ResolveImagePartType(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/png" => P.ImagePartType.Png,
            "image/gif" => P.ImagePartType.Gif,
            "image/bmp" => P.ImagePartType.Bmp,
            "image/tiff" => P.ImagePartType.Tiff,
            "image/x-icon" => P.ImagePartType.Icon,
            _ => P.ImagePartType.Jpeg
        };
    }

    private static W.Paragraph CreateParagraph(
        string text,
        string? styleId = null,
        W.JustificationValues? justification = null,
        int before = 0,
        int after = 0,
        int? fontSizeHalfPoints = null,
        bool bold = false,
        bool italic = false,
        string? color = null,
        string? background = null,
        int? line = null,
        string? languageTag = null,
        bool keepNext = false,
        bool keepLines = false)
    {
        W.ParagraphProperties properties = new();
        if (!string.IsNullOrWhiteSpace(styleId))
            properties.Append(new W.ParagraphStyleId { Val = new O.StringValue(styleId) });
        if (justification.HasValue)
            properties.Append(new W.Justification { Val = new O.EnumValue<W.JustificationValues>(justification.Value) });
        if (keepNext)
            properties.Append(new W.KeepNext());
        if (keepLines)
            properties.Append(new W.KeepLines());
        properties.Append(new W.WidowControl());
        if (before > 0 || after > 0 || line.HasValue)
        {
            W.SpacingBetweenLines spacing = new()
            {
                Before = new O.StringValue(before.ToString(CultureInfo.InvariantCulture)),
                After = new O.StringValue(after.ToString(CultureInfo.InvariantCulture))
            };
            if (line.HasValue)
            {
                spacing.Line = new O.StringValue(line.Value.ToString(CultureInfo.InvariantCulture));
                spacing.LineRule = new O.EnumValue<W.LineSpacingRuleValues>(W.LineSpacingRuleValues.Auto);
            }
            properties.Append(spacing);
        }
        if (!string.IsNullOrWhiteSpace(background))
        {
            properties.Append(new W.Shading
            {
                Fill = new O.StringValue(background),
                Val = new O.EnumValue<W.ShadingPatternValues>(W.ShadingPatternValues.Clear)
            });
            properties.Append(new W.Indentation
            {
                Left = new O.StringValue("100"),
                Right = new O.StringValue("100")
            });
        }

        string normalizedText = (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        W.Paragraph paragraph = new(properties);
        string[] lines = normalizedText.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            W.Run run = new(
                CreateRunProperties(
                    bold,
                    fontSizeHalfPoints,
                    color,
                    italic,
                    languageTag: languageTag),
                new W.Text(lines[index])
                {
                    Space = new O.EnumValue<O.SpaceProcessingModeValues>(O.SpaceProcessingModeValues.Preserve)
                });
            if (index < lines.Length - 1)
                run.Append(new W.Break());
            paragraph.Append(run);
        }

        return paragraph;
    }

    private static W.RunProperties CreateRunProperties(
        bool bold = false,
        int? fontSizeHalfPoints = null,
        string? color = null,
        bool italic = false,
        bool superscript = false,
        string? languageTag = null,
        bool noProof = true)
    {
        W.RunProperties properties = new(CreateRunFonts());
        if (bold)
            properties.Append(new W.Bold());
        if (italic)
            properties.Append(new W.Italic());
        if (superscript)
        {
            properties.Append(new W.VerticalTextAlignment
            {
                Val = new O.EnumValue<W.VerticalPositionValues>(W.VerticalPositionValues.Superscript)
            });
        }
        if (fontSizeHalfPoints.HasValue)
        {
            string size = fontSizeHalfPoints.Value.ToString(CultureInfo.InvariantCulture);
            properties.Append(new W.FontSize { Val = new O.StringValue(size) });
            properties.Append(new W.FontSizeComplexScript { Val = new O.StringValue(size) });
        }
        if (!string.IsNullOrWhiteSpace(color))
            properties.Append(new W.Color { Val = new O.StringValue(color) });
        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            properties.Append(new W.Languages
            {
                Val = new O.StringValue(languageTag),
                EastAsia = new O.StringValue(languageTag),
                Bidi = new O.StringValue(languageTag)
            });
        }
        if (noProof)
            properties.Append(new W.NoProof());
        return properties;
    }

    private static W.RunFonts CreateRunFonts()
    {
        return new W.RunFonts
        {
            Ascii = new O.StringValue(FontName),
            HighAnsi = new O.StringValue(FontName),
            EastAsia = new O.StringValue(FontName),
            ComplexScript = new O.StringValue(FontName)
        };
    }

    private static W.Table CreateTable(
        IReadOnlyList<int> columnWidths,
        bool borderless = false)
    {
        W.Table table = new();
        W.TableProperties properties = new(
            new W.TableWidth
            {
                Type = new O.EnumValue<W.TableWidthUnitValues>(W.TableWidthUnitValues.Pct),
                Width = new O.StringValue("5000")
            },
            new W.TableLayout
            {
                Type = new O.EnumValue<W.TableLayoutValues>(W.TableLayoutValues.Fixed)
            },
            CreateTableCellMargins());

        properties.Append(borderless ? CreateNilBorders() : CreateTableBorders());
        table.Append(properties);

        W.TableGrid grid = new();
        foreach (int width in columnWidths)
            grid.Append(new W.GridColumn { Width = new O.StringValue(width.ToString(CultureInfo.InvariantCulture)) });
        table.Append(grid);
        return table;
    }

    private static W.TableCellMarginDefault CreateTableCellMargins()
    {
        W.TopMargin top = new();
        SetTableMarginAttributes(top);
        W.TableCellLeftMargin left = new();
        SetTableMarginAttributes(left);
        W.BottomMargin bottom = new();
        SetTableMarginAttributes(bottom);
        W.TableCellRightMargin right = new();
        SetTableMarginAttributes(right);
        return new W.TableCellMarginDefault(top, left, bottom, right);
    }

    private static void SetTableMarginAttributes(O.OpenXmlElement margin)
    {
        const string ns = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        margin.SetAttribute(new O.OpenXmlAttribute("w", "w", ns, "70"));
        margin.SetAttribute(new O.OpenXmlAttribute("w", "type", ns, "dxa"));
    }

    private static W.TableBorders CreateTableBorders()
    {
        const string color = "C7CDD8";
        return new W.TableBorders(
            new W.TopBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single), Color = new O.StringValue(color), Size = new O.UInt32Value(4U) },
            new W.LeftBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single), Color = new O.StringValue(color), Size = new O.UInt32Value(4U) },
            new W.BottomBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single), Color = new O.StringValue(color), Size = new O.UInt32Value(4U) },
            new W.RightBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single), Color = new O.StringValue(color), Size = new O.UInt32Value(4U) },
            new W.InsideHorizontalBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single), Color = new O.StringValue(color), Size = new O.UInt32Value(4U) },
            new W.InsideVerticalBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single), Color = new O.StringValue(color), Size = new O.UInt32Value(4U) });
    }

    private static W.TableBorders CreateNilBorders()
    {
        return new W.TableBorders(
            new W.TopBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
            new W.LeftBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
            new W.BottomBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
            new W.RightBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
            new W.InsideHorizontalBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
            new W.InsideVerticalBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) });
    }

    private static W.TableRow CreateTableRow(IReadOnlyList<W.TableCell> cells, bool isHeader = false)
    {
        W.TableRow row = new();
        W.TableRowProperties properties = new(new W.CantSplit());
        if (isHeader)
            properties.Append(new W.TableHeader());
        row.Append(properties);
        foreach (W.TableCell cell in cells)
            row.Append(cell);
        return row;
    }

    private static W.TableCell CreateTableCell(
        string? text,
        int width,
        bool bold = false,
        string? background = null,
        string? textColor = null,
        bool center = false,
        bool borderless = false)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, background, borderless: borderless));
        string[] lines = (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        foreach (string line in lines)
        {
            cell.Append(CreateParagraph(
                line,
                justification: center ? W.JustificationValues.Center : W.JustificationValues.Left,
                fontSizeHalfPoints: 15,
                bold: bold,
                color: textColor,
                after: 0));
        }
        return cell;
    }

    private static W.TableCellProperties CreateCellProperties(
        int width,
        string? background = null,
        bool borderless = false)
    {
        W.TableCellProperties properties = new(
            new W.TableCellWidth
            {
                Type = new O.EnumValue<W.TableWidthUnitValues>(W.TableWidthUnitValues.Dxa),
                Width = new O.StringValue(width.ToString(CultureInfo.InvariantCulture))
            },
            new W.TableCellVerticalAlignment
            {
                Val = new O.EnumValue<W.TableVerticalAlignmentValues>(W.TableVerticalAlignmentValues.Center)
            });

        if (!string.IsNullOrWhiteSpace(background))
        {
            properties.Append(new W.Shading
            {
                Fill = new O.StringValue(background),
                Val = new O.EnumValue<W.ShadingPatternValues>(W.ShadingPatternValues.Clear)
            });
        }
        if (borderless)
        {
            properties.Append(new W.TableCellBorders(
                new W.TopBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
                new W.LeftBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
                new W.BottomBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
                new W.RightBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) }));
        }
        return properties;
    }

    private static WordTheme ResolveTheme(AbstractBookCoverTheme theme)
    {
        return theme switch
        {
            AbstractBookCoverTheme.Minimal => new WordTheme(
                "1E2024", "5F636C", "E6E8EC", "F6F6F7", "FAFAFA", "1E2024", "5A5E66", "1E2024"),
            AbstractBookCoverTheme.Editorial => new WordTheme(
                "123746", "3C6775", "C4DADA", "EDF6F5", "0B2A37", "FFFFFF", "CDE1E2", "4CCEB4"),
            _ => new WordTheme(
                "244A91", "5A6373", "D0D8E6", "F0F4FC", "193060", "FFFFFF", "D2DDF4", "4F8CFF")
        };
    }

    private static string FormatHeaderDateRange(
        DateTime? start,
        DateTime? end,
        string? culture)
    {
        if (!start.HasValue)
            return string.Empty;

        CultureInfo englishCulture = CultureInfo.GetCultureInfo("en-US");
        DateTime startDate = start.Value.Date;
        DateTime endDate = end?.Date ?? startDate;

        if (startDate == endDate)
            return $"{startDate.Day} {startDate.ToString("MMMM yyyy", englishCulture)}";

        if (startDate.Year == endDate.Year && startDate.Month == endDate.Month)
            return $"{startDate.Day}-{endDate.Day} {endDate.ToString("MMMM yyyy", englishCulture)}";

        return $"{startDate.Day} {startDate.ToString("MMMM yyyy", englishCulture)} - " +
               $"{endDate.Day} {endDate.ToString("MMMM yyyy", englishCulture)}";
    }

    private static string BuildHeaderLocation(string? city, string? venue)
    {
        string normalizedCity = city?.Trim() ?? string.Empty;
        string normalizedVenue = venue?.Trim() ?? string.Empty;

        if (string.Equals(normalizedCity, normalizedVenue, StringComparison.CurrentCultureIgnoreCase))
            return normalizedCity;

        return string.Join(" - ", new[] { normalizedCity, normalizedVenue }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string NormalizeOrcidForDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim();

        foreach (string prefix in new[]
                 {
                     "https://orcid.org/",
                     "http://orcid.org/",
                     "orcid.org/",
                     "ORCID NO:",
                     "ORCID:"
                 })
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[prefix.Length..].Trim();
                break;
            }
        }

        return normalized;
    }

    private static string FormatDateRange(DateTime? start, DateTime? end)
    {
        if (!start.HasValue)
            return string.Empty;
        if (!end.HasValue || start.Value.Date == end.Value.Date)
            return start.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        return $"{start.Value:dd.MM.yyyy} - {end.Value:dd.MM.yyyy}";
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;

    private sealed record WordTheme(
        string Primary,
        string Muted,
        string Border,
        string SoftBackground,
        string CoverBackground,
        string CoverText,
        string CoverMuted,
        string Accent);
}
