using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using O = DocumentFormat.OpenXml;
using P = DocumentFormat.OpenXml.Packaging;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Domain.Enums;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Services;

public sealed class ProgramDraftWordRenderer : IProgramDraftWordRenderer
{
    private static int _drawingIdSeed;
    private const string FontName = "Times New Roman";
    private const int HeadingFontSizeHalfPoints = 40;
    private const int ProgramFontSizeHalfPoints = 18;
    private const int BoardParticipantFontSizeHalfPoints = 18;
    private const string Heading1StyleId = "Heading1";
    private const string Heading2StyleId = "Heading2";
    private const string BookTitleStyleId = "BookTitle";
    private const string BookSubtitleStyleId = "BookSubtitle";
    private const string SmallMutedStyleId = "SmallMuted";
    private const string RendererPatchVersion = "program-book-word-v4.8-abstract-style-page-header";

    public byte[] Render(
        string congressName,
        ProgramPlanDto plan,
        string? culture,
        ProgramBookCoverDto? cover = null,
        ProgramBookRenderOptionsDto? options = null,
        string? publicBaseUrl = null,
        ProgramBookPageHeaderDto? pageHeader = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        string resolvedCongressName = string.IsNullOrWhiteSpace(congressName) ? plan.Name : congressName;
        CultureInfo cultureInfo = ResolveCulture(culture);
        ProgramBookRenderOptionsDto resolvedOptions = options ?? new ProgramBookRenderOptionsDto();
        ProgramBookPageHeaderDto resolvedPageHeader = ResolvePageHeader(
            pageHeader,
            resolvedCongressName,
            plan);

        using MemoryStream stream = new();
        using (P.WordprocessingDocument package = P.WordprocessingDocument.Create(
                   stream,
                   O.WordprocessingDocumentType.Document,
                   true))
        {
            P.MainDocumentPart mainPart = package.AddMainDocumentPart();
            mainPart.Document = new W.Document();
            W.Body body = mainPart.Document.AppendChild(new W.Body());

            AddStyles(mainPart);
            AddSettings(mainPart);
            string footerRelationshipId = AddFooter(mainPart);
            string headerRelationshipId = AddHeader(mainPart, resolvedPageHeader);

            RenderCover(body, mainPart, resolvedCongressName, plan, cover);
            // Kapak her iki durumda da (yüklenen görsel veya varsayılan kapak)
            // üstbilgi ve altbilgi içermeyen ayrı bir section olarak kapanır.
            ApplySectionPropertiesToLastParagraph(body, CreateCoverSectionProperties(nextPage: true));

            bool hasPortraitFrontSection = false;
            if (resolvedOptions.IncludeTableOfContents)
            {
                RenderContents(body);
                hasPortraitFrontSection = true;
            }

            if (resolvedOptions.IncludeBoards)
            {
                if (hasPortraitFrontSection)
                    AddPageBreak(body);
                RenderBoards(body, plan);
                hasPortraitFrontSection = true;
            }

            if (hasPortraitFrontSection)
            {
                AddSectionBreak(
                    body,
                    CreateSectionProperties(landscape: false, footerRelationshipId, headerRelationshipId, nextPage: true));
            }

            RenderProgramme(body, plan, cultureInfo, resolvedOptions);
            RenderVideoPresentations(body, mainPart, plan, publicBaseUrl);

            // All generated pages use portrait orientation.
            AddSectionBreak(body, CreateSectionProperties(landscape: false, footerRelationshipId, headerRelationshipId, nextPage: true));
            RenderParticipants(body, plan);

            body.Append(CreateSectionProperties(landscape: false, footerRelationshipId, headerRelationshipId, nextPage: false));
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
            NormalizeDocumentRelationshipsAndMedia(archive);
            NormalizeOpenXmlOrdering(archive);
        }

        return stream.ToArray();
    }

    private static void NormalizeContentTypes(ZipArchive archive)
    {
        ZipArchiveEntry? contentTypesEntry = archive.GetEntry("[Content_Types].xml");
        if (contentTypesEntry is null)
            return;

        XDocument contentTypes;
        using (Stream entryStream = contentTypesEntry.Open())
        {
            contentTypes = XDocument.Load(entryStream);
        }

        XNamespace ns = "http://schemas.openxmlformats.org/package/2006/content-types";
        XElement root = contentTypes.Root
            ?? throw new InvalidOperationException("DOCX content types manifest could not be read.");

        // Some generated packages were observed with document.xml represented by
        // a generic Default Extension="xml" entry. Microsoft Word can repair/open
        // those files, but it shows the 'unreadable content' warning. Main document
        // and known Word parts must be explicit Override entries.
        root.Elements(ns + "Default")
            .Where(element => string.Equals(
                (string?)element.Attribute("Extension"),
                "xml",
                StringComparison.OrdinalIgnoreCase))
            .Remove();

        EnsureContentTypeOverride(
            root,
            ns,
            "/word/document.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml");
        EnsureContentTypeOverride(
            root,
            ns,
            "/word/styles.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml");
        EnsureContentTypeOverride(
            root,
            ns,
            "/word/settings.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml");
        EnsureContentTypeOverride(
            root,
            ns,
            "/word/footer1.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml");
        EnsureContentTypeOverride(
            root,
            ns,
            "/word/header1.xml",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml");

        ReplaceXmlEntry(archive, "[Content_Types].xml", contentTypes);
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

    private static void NormalizeDocumentRelationshipsAndMedia(ZipArchive archive)
    {
        List<(string Source, string Destination)> mediaMoves = new();

        NormalizeRelationshipEntry(archive, "word/_rels/document.xml.rels", target =>
        {
            if (target.StartsWith("/word/", StringComparison.OrdinalIgnoreCase))
                return target[6..];

            if (target.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
            {
                string fileName = target.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
                mediaMoves.Add(($"media/{fileName}", $"word/media/{fileName}"));
                return $"media/{fileName}";
            }

            return target;
        });

        NormalizeRelationshipEntry(archive, "word/_rels/header1.xml.rels", target =>
        {
            if (target.StartsWith("/word/", StringComparison.OrdinalIgnoreCase))
                return target[6..];

            if (target.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
            {
                string fileName = target.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
                mediaMoves.Add(($"media/{fileName}", $"word/media/{fileName}"));
                return $"media/{fileName}";
            }

            return target;
        });

        foreach ((string source, string destination) in mediaMoves.Distinct())
            MoveZipEntry(archive, source, destination);
    }

    private static void NormalizeOpenXmlOrdering(ZipArchive archive)
    {
        NormalizeOpenXmlOrderingForEntry(archive, "word/document.xml");
        NormalizeOpenXmlOrderingForEntry(archive, "word/styles.xml");
        NormalizeOpenXmlOrderingForEntry(archive, "word/footer1.xml");
        NormalizeOpenXmlOrderingForEntry(archive, "word/header1.xml");
    }

    private static void NormalizeOpenXmlOrderingForEntry(ZipArchive archive, string entryName)
    {
        ZipArchiveEntry? entry = archive.GetEntry(entryName);
        if (entry is null)
            return;

        XDocument document;
        using (Stream entryStream = entry.Open())
        {
            document = XDocument.Load(entryStream, LoadOptions.PreserveWhitespace);
        }

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

    private static void NormalizeChildren(XDocument document, XName parentName, IReadOnlyList<string> expectedOrder)
    {
        Dictionary<string, int> orderMap = expectedOrder
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        foreach (XElement parent in document.Descendants(parentName).ToList())
        {
            List<XNode> nodes = parent.Nodes().ToList();
            List<XElement> sortableElements = nodes
                .OfType<XElement>()
                .Where(element => orderMap.ContainsKey(element.Name.LocalName))
                .ToList();

            if (sortableElements.Count < 2)
                continue;

            List<XElement> sortedElements = sortableElements
                .OrderBy(element => orderMap[element.Name.LocalName])
                .ToList();

            bool alreadySorted = sortableElements.SequenceEqual(sortedElements);
            if (alreadySorted)
                continue;

            parent.RemoveNodes();
            foreach (XNode node in nodes)
            {
                if (node is XElement element && orderMap.ContainsKey(element.Name.LocalName))
                    continue;

                parent.Add(node);
            }

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
        {
            document = XDocument.Load(stream);
        }

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
            if (!string.Equals(target, normalizedTarget, StringComparison.Ordinal))
            {
                relationship.SetAttributeValue("Target", normalizedTarget);
                changed = true;
            }
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
        string congressName,
        ProgramPlanDto plan,
        ProgramBookCoverDto? cover)
    {
        if (cover?.HasImage == true)
        {
            byte[] bytes = cover.ImageBytes
                ?? throw new InvalidOperationException("Kapak görseli içeriği bulunamadı.");

            W.Paragraph coverParagraph = new(
                new W.ParagraphProperties(
                    new W.SpacingBetweenLines
                    {
                        Before = new O.StringValue("0"),
                        After = new O.StringValue("0")
                    },
                    new W.Justification { Val = new O.EnumValue<W.JustificationValues>(W.JustificationValues.Center) }),
                new W.Run(CreateInlineCoverImageDrawing(
                    mainPart,
                    bytes,
                    cover.ContentType)));

            body.Append(coverParagraph);
            return;
        }

        body.Append(CreateParagraph(string.Empty, before: 2300));
        body.Append(CreateParagraph(
            congressName,
            BookTitleStyleId,
            W.JustificationValues.Center,
            after: 240));
        body.Append(CreateParagraph(
            "TASLAK PROGRAM KİTABI / DRAFT PROGRAMME BOOK",
            BookSubtitleStyleId,
            W.JustificationValues.Center,
            after: 360,
            color: "C00000"));

        if (plan.Days.Count > 0)
        {
            DateOnly first = plan.Days.Min(x => x.Date);
            DateOnly last = plan.Days.Max(x => x.Date);
            string dateText = first == last
                ? first.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                : $"{first:dd.MM.yyyy} - {last:dd.MM.yyyy}";
            body.Append(CreateParagraph(dateText, justification: W.JustificationValues.Center, fontSizeHalfPoints: 24));
        }

        body.Append(CreateParagraph(
            "TASLAK / DRAFT",
            SmallMutedStyleId,
            W.JustificationValues.Center,
            before: 1700,
            color: "969696"));
    }

    private static void RenderContents(W.Body body)
    {
        body.Append(CreateParagraph("PROGRAM KİTABI / PROGRAMME BOOK", SmallMutedStyleId, W.JustificationValues.Center));
        body.Append(CreateParagraph("İÇİNDEKİLER / CONTENTS", BookTitleStyleId, W.JustificationValues.Center, after: 80));
        body.Append(CreateParagraph(
            "Bölümler / Chapters",
            SmallMutedStyleId,
            W.JustificationValues.Center,
            after: 260));
        body.Append(CreateRuleParagraph());

        // Word rejects TOC generated as w:fldSimple in some desktop versions.
        // Keep this page static and Word-safe; a real dynamic TOC should be added
        // later as a complex field/SDT block if page-number refresh is required.
        body.Append(CreateParagraph(
            "KONGRE KURULLARI / CONGRESS BOARDS",
            justification: W.JustificationValues.Left,
            before: 220,
            after: 80,
            fontSizeHalfPoints: 20,
            color: "244A91"));
        body.Append(CreateParagraph(
            "PROGRAM / PROGRAMME",
            justification: W.JustificationValues.Left,
            after: 80,
            fontSizeHalfPoints: 20,
            color: "244A91"));
        body.Append(CreateParagraph(
            "VİDEO SUNUMLAR / VIDEO PRESENTATIONS",
            justification: W.JustificationValues.Left,
            after: 80,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            color: "244A91"));
        body.Append(CreateParagraph(
            "KATILIMCI DİZİNİ / PARTICIPANT INDEX",
            justification: W.JustificationValues.Center,
            after: 120,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            color: "244A91"));
    }

    private static void RenderBoards(W.Body body, ProgramPlanDto plan)
    {
        AppendBoardsPageTitle(body);

        IReadOnlyList<ProgramBoardSectionDto> boards = plan.BoardSections
            .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
            .ThenBy(x => x.Name)
            .ToList();

        if (boards.Count == 0)
        {
            body.Append(CreateParagraph(
                "Aktif kurul kaydı bulunamadı. / No active board record was found.",
                SmallMutedStyleId,
                W.JustificationValues.Center));
            return;
        }

        int totalMemberCount = boards.Sum(x => x.Members.Count);
        int sectionFontSize = HeadingFontSizeHalfPoints;
        int nameFontSize = BoardParticipantFontSizeHalfPoints;
        int institutionFontSize = BoardParticipantFontSizeHalfPoints;

        foreach (ProgramBoardSectionDto board in boards)
        {
            body.Append(CreateParagraph(
                board.Name,
                justification: W.JustificationValues.Center,
                fontSizeHalfPoints: sectionFontSize,
                bold: true,
                color: "244A91",
                before: 55,
                after: 15));

            IReadOnlyList<ProgramBoardMemberPdfDto> members = board.Members
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.DisplayName)
                .ToList();

            if (members.Count == 0)
                continue;

            W.Table table = CreateTable(new[] { 5000, 5000 }, borderless: true);
            for (int index = 0; index < members.Count; index += 2)
            {
                List<W.TableCell> cells = new()
                {
                    CreateBoardMemberCompactCell(members[index], 5000, nameFontSize, institutionFontSize)
                };

                cells.Add(index + 1 < members.Count
                    ? CreateBoardMemberCompactCell(members[index + 1], 5000, nameFontSize, institutionFontSize)
                    : CreateTableCell(string.Empty, 5000, borderless: true));

                table.Append(CreateTableRow(cells.ToArray()));
            }

            body.Append(table);
        }
    }

    private static void RenderProgramme(
        W.Body body,
        ProgramPlanDto plan,
        CultureInfo cultureInfo,
        ProgramBookRenderOptionsDto options)
    {
        IReadOnlyList<ProgramDayDto> printableDays = GetPrintableDays(plan);

        AppendDividerPage(body, "PROGRAM / PROGRAMME", Heading1StyleId, addPageBreakBefore: false);

        if (printableDays.Count == 0)
        {
            body.Append(CreateParagraph(
                "Programa atanmış bildiri bulunamadı. / No submission has been assigned to the programme.",
                SmallMutedStyleId,
                W.JustificationValues.Center,
                before: 180));
            return;
        }

        bool programmeContentStarted = true;

        foreach (ProgramDayDto day in printableDays)
        {
            AppendDayDividerPage(body, day);

            foreach (ProgramRoomScheduleDto room in day.Rooms
                         .Where(HasAssignedSession)
                         .OrderBy(x => x.RoomOrder)
                         .ThenBy(x => x.RoomName))
            {
                IReadOnlyList<ProgramScheduleBlockDto> printableBlocks = BuildPrintableBlocks(room);
                bool pageContextRendered = false;
                bool sessionRenderedInRoom = false;

                foreach (ProgramScheduleBlockDto block in printableBlocks)
                {
                    if (block.Kind == "fixed")
                    {
                        if (!pageContextRendered)
                        {
                            if (programmeContentStarted)
                                AddPageBreak(body);

                            AppendProgrammeRoomOnlyHeader(body, day, room.RoomName);
                            pageContextRendered = true;
                            programmeContentStarted = true;
                        }

                        W.Table fixedTable = CreateTable(new[] { 1800, 8200 }, headerBackground: "F8F2DA");
                        fixedTable.Append(CreateTableRow(new[]
                        {
                            CreateTableCell($"{block.StartTime:HH:mm}-{block.EndTime:HH:mm}", 1800, bold: true, background: "F8F2DA"),
                            CreateTableCell(block.Title, 8200, bold: true, background: "F8F2DA")
                        }));
                        body.Append(fixedTable);
                        body.Append(CreateParagraph(string.Empty, after: 40));
                        continue;
                    }

                    if (block.Session is null)
                        continue;

                    if (sessionRenderedInRoom || !pageContextRendered)
                    {
                        if (programmeContentStarted)
                            AddPageBreak(body);

                        AppendProgrammePageHeader(body, day, room.RoomName, block.Session);
                        pageContextRendered = true;
                        programmeContentStarted = true;
                    }

                    sessionRenderedInRoom = true;
                    ProgramSessionDto session = block.Session;

                    W.Table table = options.IncludeScheduleTimes
                        ? CreateTable(new[] { 1200, 3100, 5700 }, headerBackground: "E1E6EE")
                        : CreateTable(new[] { 3500, 6500 }, headerBackground: "E1E6EE");

                    table.Append(options.IncludeScheduleTimes
                        ? CreateTableRow(new[]
                        {
                            CreateTableCell("Saat / Time", 1200, bold: true, background: "E1E6EE"),
                            CreateTableCell("Yazarlar / Authors", 3100, bold: true, background: "E1E6EE"),
                            CreateTableCell("Bildiri / Submission", 5700, bold: true, background: "E1E6EE")
                        }, isHeader: true)
                        : CreateTableRow(new[]
                        {
                            CreateTableCell("Yazarlar / Authors", 3500, bold: true, background: "E1E6EE"),
                            CreateTableCell("Bildiri / Submission", 6500, bold: true, background: "E1E6EE")
                        }, isHeader: true));

                    foreach (ProgramSessionEntryDto entry in session.Entries)
                    {
                        if (entry.Kind == "break" && entry.Break is not null)
                        {
                            ProgramEmbeddedBreakDto breakEntry = entry.Break;
                            string breakLabel = options.IncludeScheduleTimes
                                ? $"{breakEntry.Title} ({breakEntry.DurationMinutes} dk)"
                                : $"{breakEntry.StartTime:HH:mm}-{breakEntry.EndTime:HH:mm} | {breakEntry.Title} ({breakEntry.DurationMinutes} dk)";

                            table.Append(options.IncludeScheduleTimes
                                ? CreateTableRow(new[]
                                {
                                    CreateTableCell($"{breakEntry.StartTime:HH:mm}-{breakEntry.EndTime:HH:mm}", 1200, bold: true, background: "F5F6F8"),
                                    CreateTableCell(breakLabel, 8800, bold: true, background: "F5F6F8", gridSpan: 2)
                                })
                                : CreateTableRow(new[]
                                {
                                    CreateTableCell(breakLabel, 10000, bold: true, background: "F5F6F8", gridSpan: 2)
                                }));
                            continue;
                        }

                        if (entry.Kind != "item" || entry.Item is null)
                            continue;

                        ProgramItemDto item = entry.Item;
                        table.Append(options.IncludeScheduleTimes
                            ? CreateTableRow(new[]
                            {
                                CreateTableCell($"{item.StartTime:HH:mm}-{item.EndTime:HH:mm}", 1200),
                                CreateTableCell(FormatAuthorsForMultiline(item.Authors), 3100),
                                CreateTableCell(item.Title, 5700)
                            })
                            : CreateTableRow(new[]
                            {
                                CreateTableCell(FormatAuthorsForMultiline(item.Authors), 3500),
                                CreateTableCell(item.Title, 6500)
                            }));
                    }

                    if (session.QuestionAnswerDurationMinutes > 0
                        && session.QuestionAnswerStartTime.HasValue
                        && session.QuestionAnswerEndTime.HasValue)
                    {
                        string questionAnswerLabel = options.IncludeScheduleTimes
                            ? $"Soru-Cevap / Questions & Answers ({session.QuestionAnswerDurationMinutes} dk)"
                            : $"{session.QuestionAnswerStartTime.Value:HH:mm}-{session.QuestionAnswerEndTime.Value:HH:mm} | Soru-Cevap / Questions & Answers ({session.QuestionAnswerDurationMinutes} dk)";

                        table.Append(options.IncludeScheduleTimes
                            ? CreateTableRow(new[]
                            {
                                CreateTableCell($"{session.QuestionAnswerStartTime.Value:HH:mm}-{session.QuestionAnswerEndTime.Value:HH:mm}", 1200, bold: true, background: "F2EEFF"),
                                CreateTableCell(questionAnswerLabel, 8800, bold: true, background: "F2EEFF", gridSpan: 2)
                            })
                            : CreateTableRow(new[]
                            {
                                CreateTableCell(questionAnswerLabel, 10000, bold: true, background: "F2EEFF", gridSpan: 2)
                            }));
                    }

                    body.Append(table);
                    body.Append(CreateParagraph(string.Empty, after: 80));
                }
            }
        }
    }

    private static void RenderVideoPresentations(
        W.Body body,
        P.MainDocumentPart mainPart,
        ProgramPlanDto plan,
        string? publicBaseUrl)
    {
        if (plan.VideoPresentations.Count == 0)
            return;

        AddPageBreak(body);
        body.Append(CreateHeading("VİDEO SUNUMLAR / VIDEO PRESENTATIONS", Heading1StyleId));
        body.Append(CreateRuleParagraph());

        W.Table table = CreateTable(new[] { 1400, 3000, 3800, 1800 }, headerBackground: "E1E6EE");
        table.Append(CreateTableRow(new[]
        {
            CreateTableCell("Bildiri No / No", 1400, bold: true, background: "E1E6EE"),
            CreateTableCell("Yazarlar / Authors", 3000, bold: true, background: "E1E6EE"),
            CreateTableCell("Bildiri / Submission", 3800, bold: true, background: "E1E6EE"),
            CreateTableCell("Bağlantı / Link", 1800, bold: true, background: "E1E6EE")
        }, isHeader: true));

        foreach (ProgramVideoPresentationDto video in plan.VideoPresentations)
        {
            string? url = BuildPublicVideoUrl(publicBaseUrl, video.ShortLinkCode);
            table.Append(CreateTableRow(new[]
            {
                CreateTableCell(video.SubmissionNumber, 1400),
                CreateTableCell(FormatAuthorsForMultiline(video.Authors), 3000),
                CreateTableCell(video.Title, 3800),
                CreateHyperlinkTableCell(mainPart, url, 1800)
            }));
        }

        body.Append(table);
        body.Append(CreateParagraph(string.Empty, after: 100));
    }

    private static string? BuildPublicVideoUrl(string? publicBaseUrl, string? shortLinkCode)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl) || string.IsNullOrWhiteSpace(shortLinkCode))
            return null;

        return $"{publicBaseUrl.TrimEnd('/')}/v/{Uri.EscapeDataString(shortLinkCode.Trim())}";
    }

    private static void AppendBoardsPageTitle(W.Body body)
    {
        body.Append(CreateParagraph(
            "KONGRE KURULLARI / CONGRESS BOARDS",
            Heading1StyleId,
            W.JustificationValues.Center,
            before: 0,
            after: 80));
        body.Append(CreateRuleParagraph());
    }

    private static IReadOnlyList<BoardListEntry> BuildBoardEntries(ProgramPlanDto plan)
    {
        List<BoardListEntry> entries = new();
        foreach (ProgramBoardSectionDto board in plan.BoardSections
                     .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                     .ThenBy(x => x.Name))
        {
            entries.Add(BoardListEntry.Section(board.Name));
            IReadOnlyList<ProgramBoardMemberPdfDto> members = board.Members
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.DisplayName)
                .ToList();

            if (members.Count == 0)
            {
                entries.Add(BoardListEntry.Member("Kayıt bulunamadı. / No record was found.", string.Empty));
                continue;
            }

            foreach (ProgramBoardMemberPdfDto member in members)
                entries.Add(BoardListEntry.Member(member.DisplayName, member.Institution));
        }

        return entries;
    }

    private static W.TableCell CreateBoardMemberCompactCell(
        ProgramBoardMemberPdfDto member,
        int width,
        int nameFontSize,
        int institutionFontSize)
    {
        W.Table nested = CreateTable(new[] { 2200, 2800 }, borderless: true);
        nested.Append(CreateTableRow(new[]
        {
            CreateBoardLineCellCustom(member.DisplayName, 2200, nameFontSize, bold: true),
            CreateBoardLineCellCustom(member.Institution, 2800, institutionFontSize)
        }));

        W.TableCell outer = new();
        outer.Append(CreateCellProperties(width, borderless: true));
        outer.Append(nested);
        return outer;
    }

    private static W.TableCell CreateBoardColumnCell(
        IReadOnlyList<BoardListEntry> entries,
        int width,
        int sectionFontSize,
        int nameFontSize,
        int institutionFontSize)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, borderless: true));

        foreach (BoardListEntry entry in entries)
        {
            if (entry.IsSection)
            {
                cell.Append(CreateParagraph(
                    entry.Name,
                    justification: W.JustificationValues.Left,
                    fontSizeHalfPoints: sectionFontSize,
                    bold: true,
                    color: "244A91",
                    before: 45,
                    after: 15));
                continue;
            }

            W.Table line = CreateTable(new[] { 2200, 2800 }, borderless: true);
            line.Append(CreateTableRow(new[]
            {
                CreateBoardLineCellCustom(entry.Name, 2200, nameFontSize, bold: true),
                CreateBoardLineCellCustom(entry.Institution, 2800, institutionFontSize)
            }));
            cell.Append(line);
        }

        return cell;
    }

    private static W.TableCell CreateBoardLineCellCustom(
        string? text,
        int width,
        int fontSizeHalfPoints,
        bool bold = false)
    {
        W.TableCell cell = new();
        cell.Append(CreateBoardLineCellProperties(width, bottomBorderSize: 3U));

        cell.Append(CreateParagraph(
            text ?? string.Empty,
            justification: W.JustificationValues.Left,
            fontSizeHalfPoints: fontSizeHalfPoints,
            bold: bold,
            color: "141C2D",
            after: 0));

        return cell;
    }

    private static void AppendProgrammePageHeader(
        W.Body body,
        ProgramDayDto day,
        string roomName,
        ProgramSessionDto session)
    {
        string topHeader = BuildProgrammeSessionTopHeader(day, session);

        body.Append(CreateParagraph(
            topHeader,
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "244A91",
            before: 40,
            after: 10));

        body.Append(CreateParagraph(
            GetBilingualSessionTitle(session.Title),
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "244A91",
            before: 0,
            after: 40));

        body.Append(CreateParagraph(
            $"{roomName}          {session.StartTime:HH:mm}-{session.EndTime:HH:mm}",
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "C00000",
            before: 10,
            after: 30));

        if (!string.IsNullOrWhiteSpace(session.ChairName)
            || !string.IsNullOrWhiteSpace(session.ViceChairName))
        {
            W.Table officials = CreateTable(new[] { 5000, 5000 }, headerBackground: "FAFBFD");
            officials.Append(CreateTableRow(new[]
            {
                CreateOfficialCell("Oturum Başkanı / Session Chair", session.ChairName, 5000),
                CreateOfficialCell("Oturum Başkan Yardımcısı / Vice Chair", session.ViceChairName, 5000)
            }));
            body.Append(officials);
            body.Append(CreateParagraph(string.Empty, after: 40));
        }
    }

    private static void AppendProgrammeRoomOnlyHeader(
        W.Body body,
        ProgramDayDto day,
        string roomName)
    {
        CultureInfo tr = new("tr-TR");
        CultureInfo en = new("en-US");

        string topHeader =
            $"{day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", tr)} / " +
            $"{day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", en)}";

        body.Append(CreateParagraph(
            topHeader,
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "244A91",
            before: 40,
            after: 40));

        body.Append(CreateParagraph(
            roomName,
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "C00000",
            before: 20,
            after: 50));
    }

    private static void AppendDividerPage(
        W.Body body,
        string title,
        string styleId,
        bool addPageBreakBefore)
    {
        if (addPageBreakBefore)
            AddPageBreak(body);

        body.Append(CreateParagraph(string.Empty, after: 5000));
        body.Append(CreateParagraph(
            title,
            styleId,
            W.JustificationValues.Center,
            after: 0));
    }

    private static void AppendDayDividerPage(W.Body body, ProgramDayDto day)
    {
        AddPageBreak(body);

        CultureInfo tr = new("tr-TR");
        CultureInfo en = new("en-US");

        string title = $"{day.Order}. GÜN / DAY {day.Order}";
        string trDate = day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd, dd MMMM yyyy", tr);
        string enDate = day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd, dd MMMM yyyy", en);

        body.Append(CreateParagraph(string.Empty, after: 4700));
        body.Append(CreateParagraph(
            title,
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "244A91",
            after: 120));

        body.Append(CreateParagraph(
            trDate,
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "244A91",
            after: 40));

        body.Append(CreateParagraph(
            enDate,
            justification: W.JustificationValues.Center,
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            bold: true,
            color: "244A91",
            after: 0));
    }

    private static string GetBilingualSessionTitle(string? title)
    {
        string value = title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (value.Contains("/", StringComparison.Ordinal))
            return value;

        Match match = Regex.Match(value, @"^(?<no>\d+)\.\s*Oturum$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success && int.TryParse(match.Groups["no"].Value, out int number))
            return $"{number}. Oturum / {number}{GetEnglishOrdinalSuffix(number)} Session";

        return value;
    }

    private static string GetEnglishOrdinalSuffix(int number)
    {
        int lastTwoDigits = number % 100;
        if (lastTwoDigits is 11 or 12 or 13)
            return "th";

        return (number % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }

    private static string BuildProgrammeSessionTopHeader(ProgramDayDto day, ProgramSessionDto session)
    {
        CultureInfo tr = new("tr-TR");
        CultureInfo en = new("en-US");

        string trDay = day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", tr);
        string enDay = day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", en);

        return $"{trDay} / {enDay}";
    }

    private static void RenderParticipants(W.Body body, ProgramPlanDto plan)
    {
        body.Append(CreateParagraph(
            "KATILIMCI DİZİNİ / PARTICIPANT INDEX",
            BookTitleStyleId,
            W.JustificationValues.Center,
            after: 120,
            color: "244A91"));
        body.Append(CreateRuleParagraph());

        if (plan.Participants.Count == 0)
        {
            body.Append(CreateParagraph(
                "Programa atanmış katılımcı bulunamadı. / No assigned participant was found.",
                justification: W.JustificationValues.Center));
            return;
        }

        W.Table table = CreateTable(new[] { 5000, 5000 }, borderless: true);
        for (int index = 0; index < plan.Participants.Count; index += 2)
        {
            List<W.TableCell> cells = new()
            {
                CreateParticipantCell(plan.Participants[index], index + 1, 5000)
            };

            cells.Add(index + 1 < plan.Participants.Count
                ? CreateParticipantCell(plan.Participants[index + 1], index + 2, 5000)
                : CreateTableCell(string.Empty, 5000, borderless: true));

            table.Append(CreateTableRow(cells.ToArray()));
        }

        body.Append(table);

    }

    private static W.TableCell CreateParticipantLineCell(ProgramParticipantDto participant, int width)
    {
        W.Table nested = CreateTable(new[] { 2200, 2800 }, borderless: true);
        nested.Append(CreateTableRow(new[]
        {
            CreateBoardLineCellCustom(participant.DisplayName, 2200, fontSizeHalfPoints: 13, bold: true),
            CreateBoardLineCellCustom(participant.Institution, 2800, fontSizeHalfPoints: 12)
        }));

        W.TableCell outer = new();
        outer.Append(CreateCellProperties(width, borderless: true));
        outer.Append(nested);
        return outer;
    }

    private static W.TableCellProperties CreateBoardLineCellProperties(int width, uint bottomBorderSize)
    {
        W.TableCellProperties properties = new(new W.TableCellWidth
        {
            Type = new O.EnumValue<W.TableWidthUnitValues>(W.TableWidthUnitValues.Dxa),
            Width = new O.StringValue(width.ToString(CultureInfo.InvariantCulture))
        });

        // Cell property order: width, borders, vertical alignment.
        properties.Append(new W.TableCellBorders(
            new W.TopBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
            new W.LeftBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
            new W.BottomBorder
            {
                Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single),
                Color = new O.StringValue("D2DAE6"),
                Size = new O.UInt32Value(bottomBorderSize)
            },
            new W.RightBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) }));
        properties.Append(new W.TableCellVerticalAlignment
        {
            Val = new O.EnumValue<W.TableVerticalAlignmentValues>(W.TableVerticalAlignmentValues.Center)
        });

        return properties;
    }

    private static W.TableCell CreateBoardLineCellCompact(string? text, int width, bool bold = false)
    {
        W.TableCell cell = new();
        cell.Append(CreateBoardLineCellProperties(width, bottomBorderSize: 4U));

        cell.Append(CreateParagraph(
            text ?? string.Empty,
            justification: W.JustificationValues.Left,
            fontSizeHalfPoints: BoardParticipantFontSizeHalfPoints,
            bold: bold,
            color: "141C2D",
            after: 0));

        return cell;
    }

    private static W.TableCell CreateBoardLineCell(string? text, int width, bool bold = false)
    {
        W.TableCell cell = new();
        cell.Append(CreateBoardLineCellProperties(width, bottomBorderSize: 4U));
        cell.Append(CreateParagraph(
            text ?? string.Empty,
            justification: W.JustificationValues.Left,
            fontSizeHalfPoints: BoardParticipantFontSizeHalfPoints,
            bold: bold,
            color: "141C2D",
            after: 0));
        return cell;
    }

    private static W.TableCell CreateOfficialCell(string label, string? value, int width)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, background: "FAFBFD"));
        cell.Append(CreateParagraph(label, fontSizeHalfPoints: ProgramFontSizeHalfPoints, bold: true, color: "5A6373", after: 20));
        cell.Append(CreateParagraph(string.IsNullOrWhiteSpace(value) ? "-" : value, fontSizeHalfPoints: ProgramFontSizeHalfPoints));
        return cell;
    }

    private static W.TableCell CreateParticipantCell(ProgramParticipantDto participant, int number, int width)
    {
        W.Table nested = CreateTable(new[] { 700, 4300 }, borderless: true);
        nested.Append(CreateTableRow(new[]
        {
            CreateTableCell(number.ToString("00", CultureInfo.InvariantCulture), 700, bold: true, background: "244A91", textColor: "FFFFFF", center: true, borderless: true),
            CreateParticipantInfoCell(participant, 4300)
        }));

        W.TableCell outer = new();
        outer.Append(CreateCellProperties(width, borderless: true));
        outer.Append(nested);
        outer.Append(CreateParagraph(string.Empty, after: 40));
        return outer;
    }

    private static W.TableCell CreateParticipantInfoCell(ProgramParticipantDto participant, int width)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, borderless: true));
        cell.Append(CreateParagraph(participant.DisplayName, fontSizeHalfPoints: BoardParticipantFontSizeHalfPoints, bold: true, color: "232D3E", after: 20));
        if (!string.IsNullOrWhiteSpace(participant.Institution))
            cell.Append(CreateParagraph(participant.Institution, fontSizeHalfPoints: BoardParticipantFontSizeHalfPoints, color: "5A6373"));
        return cell;
    }

    private static IReadOnlyList<ProgramDayDto> GetPrintableDays(ProgramPlanDto plan)
    {
        return plan.Days
            .Where(day => day.Rooms.Any(HasAssignedSession))
            .OrderBy(day => day.Order)
            .ToList();
    }

    private static bool HasAssignedSession(ProgramRoomScheduleDto room)
    {
        return room.Blocks.Any(block =>
            block.Kind == "session"
            && block.Session is { IsEmpty: false });
    }

    private static IReadOnlyList<ProgramScheduleBlockDto> BuildPrintableBlocks(ProgramRoomScheduleDto room)
    {
        List<ProgramScheduleBlockDto> assignedSessions = room.Blocks
            .Where(block => block.Kind == "session" && block.Session is { IsEmpty: false })
            .OrderBy(block => block.Session!.StartTime)
            .ToList();

        if (assignedSessions.Count == 0)
            return Array.Empty<ProgramScheduleBlockDto>();

        TimeOnly firstSessionStart = assignedSessions.Min(block => block.Session!.StartTime);
        TimeOnly lastSessionEnd = assignedSessions.Max(block => block.Session!.EndTime);

        return room.Blocks
            .Where(block => IsPrintableBlock(block, firstSessionStart, lastSessionEnd))
            .OrderBy(block => block.StartTime)
            .ThenBy(block => block.EndTime)
            .ToList();
    }

    private static bool IsPrintableBlock(
        ProgramScheduleBlockDto block,
        TimeOnly firstSessionStart,
        TimeOnly lastSessionEnd)
    {
        if (block.Kind == "session")
            return block.Session is { IsEmpty: false };

        if (block.Kind != "fixed")
            return false;

        if (block.FixedBlockType == CongressProgramFixedBlockType.Break && !block.IsPersisted)
            return false;

        return block.FixedBlockType switch
        {
            CongressProgramFixedBlockType.Opening => true,
            CongressProgramFixedBlockType.Closing => true,
            _ => block.StartTime < lastSessionEnd && block.EndTime > firstSessionStart
        };
    }

    private static void AddStyles(P.MainDocumentPart mainPart)
    {
        P.StyleDefinitionsPart stylesPart = mainPart.AddNewPart<P.StyleDefinitionsPart>();
        W.Styles styles = new();

        styles.Append(CreateParagraphStyle(
            "Normal",
            "Normal",
            fontSizeHalfPoints: ProgramFontSizeHalfPoints,
            color: "232D3E",
            isDefault: true));
        styles.Append(CreateParagraphStyle(
            BookTitleStyleId,
            "Book Title",
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            color: "244A91",
            bold: true));
        styles.Append(CreateParagraphStyle(
            BookSubtitleStyleId,
            "Book Subtitle",
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            color: "244A91",
            bold: true));
        styles.Append(CreateParagraphStyle(
            SmallMutedStyleId,
            "Small Muted",
            fontSizeHalfPoints: BoardParticipantFontSizeHalfPoints,
            color: "5A6373"));
        styles.Append(CreateParagraphStyle(
            Heading1StyleId,
            "heading 1",
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            color: "244A91",
            bold: true,
            outlineLevel: 0));
        styles.Append(CreateParagraphStyle(
            Heading2StyleId,
            "heading 2",
            fontSizeHalfPoints: HeadingFontSizeHalfPoints,
            color: "244A91",
            bold: true,
            outlineLevel: 1));

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

        W.StyleParagraphProperties paragraphProperties = new();
        if (outlineLevel.HasValue)
            paragraphProperties.Append(new W.KeepNext());

        paragraphProperties.Append(new W.SpacingBetweenLines
        {
            After = new O.StringValue("80"),
            Line = new O.StringValue("240"),
            LineRule = new O.EnumValue<W.LineSpacingRuleValues>(W.LineSpacingRuleValues.Auto)
        });

        if (outlineLevel.HasValue)
            paragraphProperties.Append(new W.OutlineLevel { Val = new O.Int32Value(outlineLevel.Value) });

        style.Append(paragraphProperties);

        string resolvedFontSize = fontSizeHalfPoints.ToString(CultureInfo.InvariantCulture);
        W.StyleRunProperties runProperties = new(CreateRunFonts());
        if (bold)
            runProperties.Append(new W.Bold());
        runProperties.Append(new W.Color { Val = new O.StringValue(color) });
        runProperties.Append(new W.FontSize { Val = new O.StringValue(resolvedFontSize) });
        runProperties.Append(new W.FontSizeComplexScript { Val = new O.StringValue(resolvedFontSize) });
        style.Append(runProperties);
        return style;
    }

    private static void AddSettings(P.MainDocumentPart mainPart)
    {
        P.DocumentSettingsPart settingsPart = mainPart.AddNewPart<P.DocumentSettingsPart>();
        // Do not enable UpdateFieldsOnOpen. Word interprets automatic field refresh
        // as a potentially external field update and displays a security prompt.
        // TOC remains available and can be refreshed manually with "Alanı Güncelle".
        settingsPart.Settings = new W.Settings(
            new W.Compatibility(
                new W.CompatibilitySetting
                {
                    Name = new O.EnumValue<W.CompatSettingNameValues>(W.CompatSettingNameValues.CompatibilityMode),
                    Uri = new O.StringValue("http://schemas.microsoft.com/office/word"),
                    Val = new O.StringValue("15")
                }));
        settingsPart.Settings.Save();
    }

    private static ProgramBookPageHeaderDto ResolvePageHeader(
        ProgramBookPageHeaderDto? pageHeader,
        string congressName,
        ProgramPlanDto plan)
    {
        DateTime? fallbackStartDate = plan.Days.Count > 0
            ? plan.Days.Min(x => x.Date).ToDateTime(TimeOnly.MinValue)
            : null;
        DateTime? fallbackEndDate = plan.Days.Count > 0
            ? plan.Days.Max(x => x.Date).ToDateTime(TimeOnly.MinValue)
            : null;

        return new ProgramBookPageHeaderDto
        {
            CongressName = FirstNonEmpty(pageHeader?.CongressName, congressName),
            CongressEnglishName = FirstNonEmpty(
                pageHeader?.CongressEnglishName,
                pageHeader?.CongressName,
                congressName),
            StartDate = pageHeader?.StartDate ?? fallbackStartDate,
            EndDate = pageHeader?.EndDate ?? fallbackEndDate,
            City = pageHeader?.City?.Trim() ?? string.Empty,
            Venue = pageHeader?.Venue?.Trim() ?? string.Empty,
            LogoBytes = pageHeader?.LogoBytes,
            LogoContentType = pageHeader?.LogoContentType
        };
    }

    private static string AddHeader(
        P.MainDocumentPart mainPart,
        ProgramBookPageHeaderDto pageHeader)
    {
        P.HeaderPart headerPart = mainPart.AddNewPart<P.HeaderPart>();
        headerPart.Header = new W.Header(CreateDocumentHeaderTable(headerPart, pageHeader));
        headerPart.Header.Save();
        return mainPart.GetIdOfPart(headerPart);
    }

    private static W.Table CreateDocumentHeaderTable(
        P.HeaderPart headerPart,
        ProgramBookPageHeaderDto pageHeader)
    {
        W.Table table = CreateTable(new[] { 1400, 7200, 1400 }, borderless: true);

        W.TableCell titleCell = new();
        titleCell.Append(CreateCellProperties(7200, borderless: true));
        titleCell.Append(CreateHeaderParagraph(
            FirstNonEmpty(pageHeader.CongressEnglishName, pageHeader.CongressName),
            W.JustificationValues.Center,
            fontSizeHalfPoints: 15,
            bold: true,
            color: "244A91"));

        string dateText = FormatHeaderDateRange(pageHeader.StartDate, pageHeader.EndDate);
        if (!string.IsNullOrWhiteSpace(dateText))
        {
            titleCell.Append(CreateHeaderParagraph(
                dateText,
                W.JustificationValues.Center,
                fontSizeHalfPoints: 13,
                color: "5A6373"));
        }

        string location = BuildHeaderLocation(pageHeader.City, pageHeader.Venue);
        if (!string.IsNullOrWhiteSpace(location))
        {
            titleCell.Append(CreateHeaderParagraph(
                location,
                W.JustificationValues.Center,
                fontSizeHalfPoints: 13,
                color: "5A6373"));
        }

        table.Append(CreateTableRow(new[]
        {
            CreateDocumentHeaderLogoCell(
                headerPart,
                pageHeader.LogoBytes,
                pageHeader.LogoContentType,
                1400,
                W.JustificationValues.Left),
            titleCell,
            CreateDocumentHeaderLogoCell(
                headerPart,
                pageHeader.LogoBytes,
                pageHeader.LogoContentType,
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
        cell.Append(CreateCellProperties(width, borderless: true));

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

    private static W.Paragraph CreateHeaderParagraph(
        string text,
        W.JustificationValues justification,
        int fontSizeHalfPoints,
        bool bold = false,
        string? color = null)
    {
        W.ParagraphProperties paragraphProperties = new(
            new W.Justification { Val = justification },
            new W.SpacingBetweenLines
            {
                Before = "0",
                After = "0",
                Line = "240",
                LineRule = W.LineSpacingRuleValues.Auto
            },
            new W.KeepLines());

        W.RunProperties runProperties = new(
            new W.RunFonts
            {
                Ascii = new O.StringValue("Arial"),
                HighAnsi = new O.StringValue("Arial"),
                EastAsia = new O.StringValue("Arial"),
                ComplexScript = new O.StringValue("Arial")
            });

        if (bold)
            runProperties.Append(new W.Bold());
        if (!string.IsNullOrWhiteSpace(color))
            runProperties.Append(new W.Color { Val = new O.StringValue(color) });

        string fontSize = fontSizeHalfPoints.ToString(CultureInfo.InvariantCulture);
        runProperties.Append(new W.FontSize { Val = new O.StringValue(fontSize) });
        runProperties.Append(new W.FontSizeComplexScript { Val = new O.StringValue(fontSize) });

        return new W.Paragraph(
            paragraphProperties,
            new W.Run(runProperties, new W.Text(text ?? string.Empty)));
    }

    private static W.Drawing CreateHeaderImageDrawing(
        P.HeaderPart headerPart,
        byte[] imageBytes,
        string? contentType,
        long widthEmus,
        long heightEmus)
    {
        P.PartTypeInfo imagePartType = ResolveImagePartType(contentType);
        P.ImagePart imagePart = headerPart.AddImagePart(imagePartType);
        using (MemoryStream imageStream = new(imageBytes))
            imagePart.FeedData(imageStream);

        string relationshipId = headerPart.GetIdOfPart(imagePart)
            ?? throw new InvalidOperationException("Program kitabı üstbilgi logosu Word paketine eklenemedi.");
        uint drawingId = unchecked((uint)Interlocked.Increment(ref _drawingIdSeed));

        A.Graphic graphic = new(
            new A.GraphicData(
                new PIC.Picture(
                    new PIC.NonVisualPictureProperties(
                        new PIC.NonVisualDrawingProperties
                        {
                            Id = drawingId,
                            Name = $"Program Header Logo {drawingId}"
                        },
                        new PIC.NonVisualPictureDrawingProperties()),
                    new PIC.BlipFill(
                        new A.Blip
                        {
                            Embed = relationshipId,
                            CompressionState = A.BlipCompressionValues.Print
                        },
                        new A.Stretch(new A.FillRectangle())),
                    new PIC.ShapeProperties(
                        new A.Transform2D(
                            new A.Offset { X = 0L, Y = 0L },
                            new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                        new A.PresetGeometry(new A.AdjustValueList())
                        {
                            Preset = A.ShapeTypeValues.Rectangle
                        })))
            {
                Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
            });

        DW.Inline inline = new(
            new DW.Extent { Cx = widthEmus, Cy = heightEmus },
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
                Name = $"Program Header Logo {drawingId}"
            },
            new DW.NonVisualGraphicFrameDrawingProperties(
                new A.GraphicFrameLocks { NoChangeAspect = true }),
            graphic)
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U
        };

        return new W.Drawing(inline);
    }

    private static string FormatHeaderDateRange(DateTime? start, DateTime? end)
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

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;

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
                new W.ParagraphProperties(new W.Justification { Val = new O.EnumValue<W.JustificationValues>(W.JustificationValues.Center) }),
                new W.Run(CreateRunProperties(fontSizeHalfPoints: 14, color: "808080"), new W.Text("- ")),
                pageField,
                new W.Run(CreateRunProperties(fontSizeHalfPoints: 14, color: "808080"), new W.Text(" -"))));
        footerPart.Footer.Save();
        return mainPart.GetIdOfPart(footerPart);
    }

    private static W.SectionProperties CreateSectionProperties(
        bool landscape,
        string footerRelationshipId,
        string headerRelationshipId,
        bool nextPage)
    {
        // Match the structure Word produced after manual Save As.
        // Do not emit explicit w:type="nextPage"; a section break paragraph
        // already creates the next section and Word normalizes it without w:type.
        // The explicit w:type was one of the remaining differences between the
        // generated file and the Word-repaired file.
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

        W.PageSize pageSize = landscape
            ? new W.PageSize
            {
                Width = new O.UInt32Value(16838U),
                Height = new O.UInt32Value(11906U),
                Orient = new O.EnumValue<W.PageOrientationValues>(W.PageOrientationValues.Landscape)
            }
            : new W.PageSize
            {
                Width = new O.UInt32Value(11906U),
                Height = new O.UInt32Value(16838U)
            };

        section.Append(pageSize);
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
        section.Append(new W.Columns { Space = new O.StringValue("708") });

        return section;
    }

    private static W.SectionProperties CreateCoverSectionProperties(bool nextPage)
    {
        // Same principle as CreateSectionProperties: keep the cover section
        // minimal and Word-normalized. Manual Save As removes explicit w:type
        // from this section as well.
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

    private static void ApplySectionPropertiesToLastParagraph(W.Body body, W.SectionProperties sectionProperties)
    {
        W.Paragraph? paragraph = body.Elements<W.Paragraph>().LastOrDefault();
        if (paragraph is null)
        {
            AddSectionBreak(body, sectionProperties);
            return;
        }

        W.ParagraphProperties properties = paragraph.GetFirstChild<W.ParagraphProperties>();
        if (properties is null)
        {
            properties = new W.ParagraphProperties();
            paragraph.PrependChild(properties);
        }

        properties.RemoveAllChildren<W.SectionProperties>();
        properties.Append(sectionProperties);
    }

    private static void AddSectionBreak(W.Body body, W.SectionProperties sectionProperties)
    {
        body.Append(new W.Paragraph(new W.ParagraphProperties(sectionProperties)));
    }

    private static void AddPageBreak(W.Body body)
    {
        body.Append(new W.Paragraph(new W.Run(new W.Break { Type = new O.EnumValue<W.BreakValues>(W.BreakValues.Page) })));
    }

    private static W.Paragraph CreateHeading(string text, string styleId, string? background = null)
    {
        return CreateParagraph(
            text,
            styleId,
            W.JustificationValues.Center,
            before: 120,
            after: 120,
            background: background);
    }

    private static W.Paragraph CreateRuleParagraph()
    {
        W.ParagraphProperties properties = new(
            new W.ParagraphBorders(
                new W.BottomBorder
                {
                    Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Single),
                    Color = new O.StringValue("244A91"),
                    Size = new O.UInt32Value(14U),
                    Space = new O.UInt32Value(1U)
                }),
            new W.SpacingBetweenLines { After = new O.StringValue("80") });
        return new W.Paragraph(properties, new W.Run(new W.Text(string.Empty)));
    }

    private static W.Drawing CreateInlineCoverImageDrawing(
        P.MainDocumentPart mainPart,
        byte[] imageBytes,
        string? contentType)
    {
        // Cover is rendered as a normal inline picture inside a dedicated
        // zero-margin first section. Use full A4 extents so the uploaded cover
        // occupies the entire page without the white printable-area margins.
        const long imageWidthEmus = 7560310L;
        const long imageHeightEmus = 10692130L;

        P.PartTypeInfo imagePartType = ResolveImagePartType(contentType);
        P.ImagePart imagePart = mainPart.AddImagePart(imagePartType);
        using (MemoryStream imageStream = new(imageBytes))
            imagePart.FeedData(imageStream);

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
                            Name = $"Program Cover {drawingId}"
                        },
                        new PIC.NonVisualPictureDrawingProperties()),
                    new PIC.BlipFill(
                        new A.Blip
                        {
                            Embed = relationshipId,
                            CompressionState = A.BlipCompressionValues.Print
                        },
                        CreateCoverSourceRectangle(imageBytes),
                        new A.Stretch(new A.FillRectangle())),
                    new PIC.ShapeProperties(
                        new A.Transform2D(
                            new A.Offset { X = 0L, Y = 0L },
                            new A.Extents { Cx = imageWidthEmus, Cy = imageHeightEmus }),
                        new A.PresetGeometry(new A.AdjustValueList())
                        {
                            Preset = A.ShapeTypeValues.Rectangle
                        })))
            {
                Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
            });

        DW.Inline inline = new(
            new DW.Extent { Cx = imageWidthEmus, Cy = imageHeightEmus },
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
                Name = $"Program Cover {drawingId}"
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

    private static A.SourceRectangle CreateCoverSourceRectangle(byte[] imageBytes)
    {
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

        // Crop from the centre instead of stretching. DrawingML crop values
        // are percentages expressed in 1/1000 percent units (100000 = 100%).
        if (sourceRatio > targetRatio)
        {
            double visibleRatio = targetRatio / sourceRatio;
            int crop = (int)Math.Round(
                (1d - visibleRatio) * 50000d,
                MidpointRounding.AwayFromZero);
            left = crop;
            right = crop;
        }
        else if (sourceRatio < targetRatio)
        {
            double visibleRatio = sourceRatio / targetRatio;
            int crop = (int)Math.Round(
                (1d - visibleRatio) * 50000d,
                MidpointRounding.AwayFromZero);
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
    {
        return (bytes[offset] << 24)
            | (bytes[offset + 1] << 16)
            | (bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    private static P.PartTypeInfo ResolveImagePartType(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/png" => P.ImagePartType.Png,
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
        string? color = null,
        string? background = null)
    {
        W.ParagraphProperties properties = new();

        // Keep paragraph properties in WordprocessingML schema order.
        // Microsoft Word is stricter than LibreOffice and can show
        // "unreadable content" when spacing/shading/justification are
        // appended in arbitrary order.
        if (!string.IsNullOrWhiteSpace(styleId))
            properties.Append(new W.ParagraphStyleId { Val = new O.StringValue(styleId) });

        if (!string.IsNullOrWhiteSpace(background))
        {
            properties.Append(new W.Shading
            {
                Fill = new O.StringValue(background),
                Val = new O.EnumValue<W.ShadingPatternValues>(W.ShadingPatternValues.Clear)
            });
        }

        if (before > 0 || after > 0)
        {
            properties.Append(new W.SpacingBetweenLines
            {
                Before = new O.StringValue(before.ToString(CultureInfo.InvariantCulture)),
                After = new O.StringValue(after.ToString(CultureInfo.InvariantCulture))
            });
        }

        if (justification.HasValue)
        {
            properties.Append(new W.Justification
            {
                Val = new O.EnumValue<W.JustificationValues>(justification.Value)
            });
        }

        W.Run run = new(
            CreateRunProperties(bold, fontSizeHalfPoints, color),
            new W.Text(text ?? string.Empty)
            {
                Space = new O.EnumValue<O.SpaceProcessingModeValues>(
                    O.SpaceProcessingModeValues.Preserve)
            });
        return new W.Paragraph(properties, run);
    }

    private static W.RunProperties CreateRunProperties(
        bool bold = false,
        int? fontSizeHalfPoints = null,
        string? color = null)
    {
        W.RunProperties properties = new(CreateRunFonts());

        // Run properties must also follow schema order: fonts, bold, color, size.
        if (bold)
            properties.Append(new W.Bold());

        if (!string.IsNullOrWhiteSpace(color))
            properties.Append(new W.Color { Val = new O.StringValue(color) });

        if (fontSizeHalfPoints.HasValue)
        {
            string size = fontSizeHalfPoints.Value.ToString(CultureInfo.InvariantCulture);
            properties.Append(new W.FontSize { Val = new O.StringValue(size) });
            properties.Append(new W.FontSizeComplexScript { Val = new O.StringValue(size) });
        }

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
        string? headerBackground = null,
        bool borderless = false)
    {
        W.Table table = new();
        W.TableProperties properties = new(
            new W.TableWidth
            {
                Type = new O.EnumValue<W.TableWidthUnitValues>(W.TableWidthUnitValues.Pct),
                Width = new O.StringValue("5000")
            });

        // Word expects table properties in schema order: width, borders, layout, cell margins.
        properties.Append(borderless ? CreateNilBorders() : CreateTableBorders());
        properties.Append(new W.TableLayout
        {
            Type = new O.EnumValue<W.TableLayoutValues>(W.TableLayoutValues.Fixed)
        });
        properties.Append(CreateTableCellMargins());
        table.Append(properties);

        W.TableGrid grid = new();
        foreach (int width in columnWidths)
            grid.Append(new W.GridColumn { Width = new O.StringValue(width.ToString(CultureInfo.InvariantCulture)) });
        table.Append(grid);
        return table;
    }

    private static W.TableCellMarginDefault CreateTableCellMargins()
    {
        // Open XML SDK 3.x exposes different strongly typed width wrappers for
        // top/bottom and left/right margin elements. Writing the schema attributes
        // directly avoids the incompatible Int16Value/StringValue and EnumValue
        // assignments while still producing valid w:tblCellMar markup.
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
        const string wordprocessingNamespace =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        margin.SetAttribute(new O.OpenXmlAttribute(
            "w",
            "w",
            wordprocessingNamespace,
            "70"));

        margin.SetAttribute(new O.OpenXmlAttribute(
            "w",
            "type",
            wordprocessingNamespace,
            "dxa"));
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

    private static string FormatAuthorsForMultiline(string? authors)
    {
        if (string.IsNullOrWhiteSpace(authors))
            return string.Empty;

        string[] authorNames = authors
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split(new[] { "\n", " - " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(" - ", authorNames);
    }

    private static W.TableCell CreateTableCell(
        string? text,
        int width,
        bool bold = false,
        string? background = null,
        string? textColor = null,
        bool center = false,
        int gridSpan = 1,
        bool borderless = false)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, background, gridSpan, borderless));

        string[] lines = (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        foreach (string line in lines)
        {
            cell.Append(CreateParagraph(
                line,
                justification: center ? W.JustificationValues.Center : W.JustificationValues.Left,
                fontSizeHalfPoints: ProgramFontSizeHalfPoints,
                bold: bold,
                color: textColor,
                after: 0));
        }

        if (lines.Length == 0)
            cell.Append(CreateParagraph(string.Empty));
        return cell;
    }

    private static W.TableCell CreateRightAlignedTableCell(
        string? text,
        int width,
        bool bold = false,
        string? background = null,
        string? textColor = null,
        int gridSpan = 1,
        bool borderless = false)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width, background, gridSpan, borderless));
        cell.Append(CreateParagraph(
            text ?? string.Empty,
            justification: W.JustificationValues.Right,
            fontSizeHalfPoints: ProgramFontSizeHalfPoints,
            bold: bold,
            color: textColor,
            after: 0));
        return cell;
    }

    private static W.TableCell CreateHyperlinkTableCell(
        P.MainDocumentPart mainPart,
        string? url,
        int width)
    {
        W.TableCell cell = new();
        cell.Append(CreateCellProperties(width));

        if (string.IsNullOrWhiteSpace(url))
        {
            cell.Append(CreateParagraph(
                "Bağlantı yok / Unavailable",
                fontSizeHalfPoints: ProgramFontSizeHalfPoints,
                color: "808080",
                after: 0));
            return cell;
        }

        var relationship = mainPart.AddHyperlinkRelationship(new Uri(url), true);
        W.RunProperties runProperties = CreateRunProperties(fontSizeHalfPoints: ProgramFontSizeHalfPoints, color: "244A91");
        runProperties.Append(new W.Underline
        {
            Val = new O.EnumValue<W.UnderlineValues>(W.UnderlineValues.Single)
        });

        W.Hyperlink hyperlink = new(
            new W.Run(
                runProperties,
                new W.Text("Videoyu Aç / Open Video")))
        {
            Id = new O.StringValue(relationship.Id),
            History = new O.OnOffValue(true)
        };

        cell.Append(new W.Paragraph(
            new W.ParagraphProperties(
                new W.SpacingBetweenLines { After = new O.StringValue("0") }),
            hyperlink));
        return cell;
    }

    private static W.TableCellProperties CreateCellProperties(
        int width,
        string? background = null,
        int gridSpan = 1,
        bool borderless = false)
    {
        W.TableCellProperties properties = new(new W.TableCellWidth
        {
            Type = new O.EnumValue<W.TableWidthUnitValues>(W.TableWidthUnitValues.Dxa),
            Width = new O.StringValue(width.ToString(CultureInfo.InvariantCulture))
        });

        // WordprocessingML cell property order is tcW, gridSpan, tcBorders, shd, vAlign.
        if (gridSpan > 1)
            properties.Append(new W.GridSpan { Val = new O.Int32Value(gridSpan) });

        if (borderless)
        {
            properties.Append(new W.TableCellBorders(
                new W.TopBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
                new W.LeftBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
                new W.BottomBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) },
                new W.RightBorder { Val = new O.EnumValue<W.BorderValues>(W.BorderValues.Nil) }));
        }

        if (!string.IsNullOrWhiteSpace(background))
        {
            properties.Append(new W.Shading
            {
                Fill = new O.StringValue(background),
                Val = new O.EnumValue<W.ShadingPatternValues>(W.ShadingPatternValues.Clear)
            });
        }

        properties.Append(new W.TableCellVerticalAlignment
        {
            Val = new O.EnumValue<W.TableVerticalAlignmentValues>(W.TableVerticalAlignmentValues.Center)
        });

        return properties;
    }

    private sealed record BoardListEntry(bool IsSection, string Name, string Institution)
    {
        public static BoardListEntry Section(string name) => new(true, name, string.Empty);
        public static BoardListEntry Member(string name, string? institution) => new(false, name, institution ?? string.Empty);
    }

    private static CultureInfo ResolveCulture(string? culture)
    {
        try
        {
            return CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(culture) ? "tr-TR" : culture);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("tr-TR");
        }
    }
}
