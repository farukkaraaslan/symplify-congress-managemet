using System.Globalization;
using System.Text.RegularExpressions;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Domain.Enums;
using IOPath = System.IO.Path;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Services;

public sealed class ProgramDraftPdfRenderer : IProgramDraftPdfRenderer
{
    private const float HeadingFontSize = 20f;
    private const float ProgramFontSize = 9f;
    private const float BoardParticipantFontSize = 9f;
    public byte[] Render(
        string congressName,
        ProgramPlanDto plan,
        string? culture,
        ProgramBookCoverDto? cover = null,
        ProgramBookRenderOptionsDto? options = null,
        string? publicBaseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        string resolvedCongressName = string.IsNullOrWhiteSpace(congressName) ? plan.Name : congressName;
        CultureInfo cultureInfo = ResolveCulture(culture);
        ProgramBookRenderOptionsDto resolvedOptions = options ?? new ProgramBookRenderOptionsDto();
        BodyRenderResult body = RenderBody(
            resolvedCongressName,
            plan,
            cultureInfo,
            resolvedOptions,
            publicBaseUrl);

        int expectedFrontPageCount = resolvedOptions.IncludeTableOfContents ? 2 : 1;
        FrontRenderResult front = RenderFront(
            resolvedCongressName,
            plan,
            body.TocEntries,
            expectedFrontPageCount,
            cover,
            resolvedOptions.IncludeTableOfContents);
        for (int attempt = 0; attempt < 2 && front.PageCount != expectedFrontPageCount; attempt++)
        {
            expectedFrontPageCount = front.PageCount;
            front = RenderFront(
                resolvedCongressName,
                plan,
                body.TocEntries,
                expectedFrontPageCount,
                cover,
                resolvedOptions.IncludeTableOfContents);
        }

        return MergeAndNumber(front.Content, body.Content);
    }

    private static BodyRenderResult RenderBody(
        string congressName,
        ProgramPlanDto plan,
        CultureInfo cultureInfo,
        ProgramBookRenderOptionsDto options,
        string? publicBaseUrl)
    {
        using MemoryStream stream = new();
        using PdfWriter writer = new(stream);
        using PdfDocument pdf = new(writer);
        using Document document = new(pdf, PageSize.A4);

        document.SetMargins(28, 28, 28, 28);
        PdfFont regular = CreateFont(bold: false);
        PdfFont bold = CreateFont(bold: true);
        List<TocEntry> tocEntries = new();

        if (options.IncludeBoards)
            RenderBoards(document, pdf, plan, regular, bold, tocEntries);

        RenderProgramme(document, pdf, plan, cultureInfo, regular, bold, tocEntries, options);
        RenderVideoPresentations(document, pdf, plan, regular, bold, tocEntries, publicBaseUrl);
        RenderParticipants(document, pdf, plan, regular, bold, tocEntries);

        document.Add(new Paragraph($"Taslak oluşturma zamanı: {DateTime.Now:dd.MM.yyyy HH:mm}")
            .SetFont(regular)
            .SetFontSize(7)
            .SetFontColor(ColorConstants.GRAY)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetMarginTop(10));

        document.Close();
        return new BodyRenderResult(stream.ToArray(), tocEntries);
    }

    private static void RenderBoards(
        Document document,
        PdfDocument pdf,
        ProgramPlanDto plan,
        PdfFont regular,
        PdfFont bold,
        ICollection<TocEntry> tocEntries)
    {
        float oldTop = document.GetTopMargin();
        float oldRight = document.GetRightMargin();
        float oldBottom = document.GetBottomMargin();
        float oldLeft = document.GetLeftMargin();
        document.SetMargins(18, 24, 18, 24);

        try
        {
            AddBoardsPageTitle(document, bold);
            tocEntries.Add(new TocEntry("Kongre Kurulları / Congress Boards", pdf.GetNumberOfPages(), 0));

            IReadOnlyList<ProgramBoardSectionDto> boards = plan.BoardSections
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Name)
                .ToList();

            if (boards.Count == 0)
            {
                document.Add(new Paragraph("Aktif kurul kaydı bulunamadı. / No active board record was found.")
                    .SetFont(regular)
                    .SetFontSize(ProgramFontSize)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(4)
                    .SetMarginBottom(0));
                return;
            }

            int totalMemberCount = boards.Sum(x => x.Members.Count);
            float sectionFontSize = HeadingFontSize;
            float nameFontSize = BoardParticipantFontSize;
            float institutionFontSize = BoardParticipantFontSize;
            float rowPadding = totalMemberCount > 95 ? 0.9f : totalMemberCount > 75 ? 1.1f : 1.4f;

            foreach (ProgramBoardSectionDto board in boards)
            {
                document.Add(new Paragraph(board.Name)
                    .SetFont(bold)
                    .SetFontSize(sectionFontSize)
                    .SetFontColor(new DeviceRgb(36, 74, 145))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(5)
                    .SetMarginBottom(2));
                tocEntries.Add(new TocEntry(board.Name, pdf.GetNumberOfPages(), 1));

                IReadOnlyList<ProgramBoardMemberPdfDto> members = board.Members
                    .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                    .ThenBy(x => x.DisplayName)
                    .ToList();

                if (members.Count == 0)
                    continue;

                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetBorder(Border.NO_BORDER)
                    .SetMarginTop(0)
                    .SetMarginBottom(3);

                for (int index = 0; index < members.Count; index += 2)
                {
                    table.AddCell(CreateBoardMemberCompactCell(
                        members[index],
                        regular,
                        bold,
                        nameFontSize,
                        institutionFontSize,
                        rowPadding));

                    if (index + 1 < members.Count)
                    {
                        table.AddCell(CreateBoardMemberCompactCell(
                            members[index + 1],
                            regular,
                            bold,
                            nameFontSize,
                            institutionFontSize,
                            rowPadding));
                    }
                    else
                    {
                        table.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                    }
                }

                document.Add(table);
            }
        }
        finally
        {
            document.SetMargins(oldTop, oldRight, oldBottom, oldLeft);
        }
    }

    private static void RenderProgramme(
        Document document,
        PdfDocument pdf,
        ProgramPlanDto plan,
        CultureInfo cultureInfo,
        PdfFont regular,
        PdfFont bold,
        ICollection<TocEntry> tocEntries,
        ProgramBookRenderOptionsDto options)
    {
        IReadOnlyList<ProgramDayDto> printableDays = GetPrintableDays(plan);

        pdf.SetDefaultPageSize(PageSize.A4);
        AddDividerPage(document, pdf, "PROGRAM / PROGRAMME", bold);
        tocEntries.Add(new TocEntry("Program / Programme", pdf.GetNumberOfPages(), 0));

        if (printableDays.Count == 0)
        {
            document.Add(new Paragraph("Programa atanmış bildiri bulunamadı. / No submission has been assigned to the programme.")
                .SetFont(regular)
                .SetFontSize(8.6f)
                .SetFontColor(ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(18));
            return;
        }

        bool programmeContentStarted = true;

        foreach (ProgramDayDto day in printableDays)
        {
            AddDayDividerPage(document, pdf, day, bold);
            tocEntries.Add(new TocEntry($"{day.Order}. Gün / Day {day.Order}", pdf.GetNumberOfPages(), 1));

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
                                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                            AddProgrammeRoomOnlyHeader(document, day, room.RoomName, bold);
                            pageContextRendered = true;
                            programmeContentStarted = true;
                        }

                        Table fixedTable = new Table(UnitValue.CreatePercentArray(new float[] { 18, 82 }))
                            .UseAllAvailableWidth()
                            .SetMarginBottom(5);

                        fixedTable.AddCell(CreateCell(
                            $"{block.StartTime:HH:mm}-{block.EndTime:HH:mm}",
                            bold,
                            7.6f,
                            new DeviceRgb(248, 242, 218)));

                        fixedTable.AddCell(CreateCell(
                            block.Title,
                            bold,
                            7.6f,
                            new DeviceRgb(248, 242, 218)));

                        document.Add(fixedTable);
                        continue;
                    }

                    if (block.Session is null)
                        continue;

                    if (sessionRenderedInRoom || !pageContextRendered)
                    {
                        if (programmeContentStarted)
                            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                        AddProgrammePageHeader(
                            document,
                            day,
                            room.RoomName,
                            block.Session,
                            regular,
                            bold);
                        pageContextRendered = true;
                        programmeContentStarted = true;
                    }

                    sessionRenderedInRoom = true;
                    ProgramSessionDto session = block.Session;

                    Table table = new Table(UnitValue.CreatePercentArray(
                            options.IncludeScheduleTimes
                                ? new float[] { 12, 31, 57 }
                                : new float[] { 35, 65 }))
                        .UseAllAvailableWidth()
                        .SetMarginBottom(8);

                    if (options.IncludeScheduleTimes)
                        table.AddHeaderCell(CreateCell("Saat / Time", bold, ProgramFontSize, new DeviceRgb(225, 230, 238)));
                    table.AddHeaderCell(CreateCell("Yazarlar / Authors", bold, ProgramFontSize, new DeviceRgb(225, 230, 238)));
                    table.AddHeaderCell(CreateCell("Bildiri / Submission", bold, ProgramFontSize, new DeviceRgb(225, 230, 238)));

                    foreach (ProgramSessionEntryDto entry in session.Entries)
                    {
                        if (entry.Kind == "break" && entry.Break is not null)
                        {
                            ProgramEmbeddedBreakDto breakEntry = entry.Break;
                            if (options.IncludeScheduleTimes)
                            {
                                table.AddCell(CreateCell(
                                    $"{breakEntry.StartTime:HH:mm}-{breakEntry.EndTime:HH:mm}",
                                    bold,
                                    7.0f,
                                    new DeviceRgb(245, 246, 248)));
                            }

                            string breakLabel = options.IncludeScheduleTimes
                                ? $"{breakEntry.Title} ({breakEntry.DurationMinutes} dk)"
                                : $"{breakEntry.StartTime:HH:mm}-{breakEntry.EndTime:HH:mm} | {breakEntry.Title} ({breakEntry.DurationMinutes} dk)";

                            Cell breakCell = new Cell(1, 2)
                                .Add(new Paragraph(breakLabel)
                                    .SetFont(bold)
                                    .SetFontSize(ProgramFontSize)
                                    .SetMargin(0))
                                .SetPadding(5)
                                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                                .SetBackgroundColor(new DeviceRgb(245, 246, 248));
                            table.AddCell(breakCell);
                            continue;
                        }

                        if (entry.Kind != "item" || entry.Item is null)
                            continue;

                        ProgramItemDto item = entry.Item;
                        if (options.IncludeScheduleTimes)
                            table.AddCell(CreateCell($"{item.StartTime:HH:mm}-{item.EndTime:HH:mm}", regular, ProgramFontSize));
                        table.AddCell(CreateAuthorsCell(item.Authors, regular, ProgramFontSize));
                        table.AddCell(CreateCell(item.Title, regular, ProgramFontSize));
                    }

                    if (session.QuestionAnswerDurationMinutes > 0
                        && session.QuestionAnswerStartTime.HasValue
                        && session.QuestionAnswerEndTime.HasValue)
                    {
                        if (options.IncludeScheduleTimes)
                        {
                            table.AddCell(CreateCell(
                                $"{session.QuestionAnswerStartTime.Value:HH:mm}-{session.QuestionAnswerEndTime.Value:HH:mm}",
                                bold,
                                7.0f,
                                new DeviceRgb(242, 238, 255)));
                        }

                        string questionAnswerLabel = options.IncludeScheduleTimes
                            ? $"Soru-Cevap / Questions & Answers ({session.QuestionAnswerDurationMinutes} dk)"
                            : $"{session.QuestionAnswerStartTime.Value:HH:mm}-{session.QuestionAnswerEndTime.Value:HH:mm} | Soru-Cevap / Questions & Answers ({session.QuestionAnswerDurationMinutes} dk)";

                        Cell questionCell = new Cell(1, 2)
                            .Add(new Paragraph(questionAnswerLabel)
                                .SetFont(bold)
                                .SetFontSize(ProgramFontSize)
                                .SetMargin(0))
                            .SetPadding(5)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(new DeviceRgb(242, 238, 255));
                        table.AddCell(questionCell);
                    }

                    document.Add(table);
                }
            }
        }
    }

    private static void RenderVideoPresentations(
        Document document,
        PdfDocument pdf,
        ProgramPlanDto plan,
        PdfFont regular,
        PdfFont bold,
        ICollection<TocEntry> tocEntries,
        string? publicBaseUrl)
    {
        if (plan.VideoPresentations.Count == 0)
            return;

        pdf.SetDefaultPageSize(PageSize.A4);
        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
        AddBookChapterHeading(
            document,
            "PROGRAM KİTABI / PROGRAMME BOOK",
            "VİDEO SUNUMLAR / VIDEO PRESENTATIONS",
            string.Empty,
            regular,
            bold);
        tocEntries.Add(new TocEntry("Video Sunumlar / Video Presentations", pdf.GetNumberOfPages(), 0));

        Table table = new Table(UnitValue.CreatePercentArray(new float[] { 14, 30, 38, 18 }))
            .UseAllAvailableWidth()
            .SetMarginTop(8)
            .SetMarginBottom(8);
        DeviceRgb headerColor = new(225, 230, 238);
        table.AddHeaderCell(CreateCell("Bildiri No / No", bold, ProgramFontSize, headerColor));
        table.AddHeaderCell(CreateCell("Yazarlar / Authors", bold, ProgramFontSize, headerColor));
        table.AddHeaderCell(CreateCell("Bildiri / Submission", bold, ProgramFontSize, headerColor));
        table.AddHeaderCell(CreateCell("Bağlantı / Link", bold, ProgramFontSize, headerColor));

        foreach (ProgramVideoPresentationDto video in plan.VideoPresentations)
        {
            table.AddCell(CreateCell(video.SubmissionNumber, regular, ProgramFontSize));
            table.AddCell(CreateAuthorsCell(video.Authors, regular, ProgramFontSize));
            table.AddCell(CreateCell(video.Title, regular, ProgramFontSize));

            string? url = BuildPublicVideoUrl(publicBaseUrl, video.ShortLinkCode);
            Cell linkCell = new Cell()
                .SetPadding(5)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            if (string.IsNullOrWhiteSpace(url))
            {
                linkCell.Add(new Paragraph("Bağlantı hazırlanamadı / Link unavailable")
                    .SetFont(regular)
                    .SetFontSize(ProgramFontSize)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMargin(0));
            }
            else
            {
                Link link = new("Videoyu Aç / Open Video", PdfAction.CreateURI(url));
                link.SetFont(regular);
                link.SetFontSize(ProgramFontSize);
                link.SetFontColor(new DeviceRgb(36, 74, 145));
                linkCell.Add(new Paragraph().Add(link).SetMargin(0));
            }

            table.AddCell(linkCell);
        }

        document.Add(table);
    }

    private static string? BuildPublicVideoUrl(string? publicBaseUrl, string? shortLinkCode)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl) || string.IsNullOrWhiteSpace(shortLinkCode))
            return null;

        return $"{publicBaseUrl.TrimEnd('/')}/v/{Uri.EscapeDataString(shortLinkCode.Trim())}";
    }

    private static void RenderParticipants(
        Document document,
        PdfDocument pdf,
        ProgramPlanDto plan,
        PdfFont regular,
        PdfFont bold,
        ICollection<TocEntry> tocEntries)
    {
        pdf.SetDefaultPageSize(PageSize.A4);
        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
        document.Add(new Paragraph("KATILIMCI DİZİNİ / PARTICIPANT INDEX")
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(0)
            .SetMarginBottom(4));
        AddBlueRule(document);
        tocEntries.Add(new TocEntry("Katılımcı Dizini", pdf.GetNumberOfPages(), 0));

        if (plan.Participants.Count == 0)
        {
            document.Add(new Paragraph("Programa atanmış katılımcı bulunamadı. / No assigned participant was found.")
                .SetFont(regular)
                .SetFontSize(BoardParticipantFontSize)
                .SetTextAlignment(TextAlignment.CENTER));
            return;
        }

        Table participantTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMarginTop(0)
            .SetMarginBottom(0);

        for (int index = 0; index < plan.Participants.Count; index += 2)
        {
            participantTable.AddCell(CreateParticipantCell(plan.Participants[index], index + 1, regular, bold));

            if (index + 1 < plan.Participants.Count)
                participantTable.AddCell(CreateParticipantCell(plan.Participants[index + 1], index + 2, regular, bold));
            else
                participantTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
        }

        document.Add(participantTable);
    }

    private static FrontRenderResult RenderFront(
        string congressName,
        ProgramPlanDto plan,
        IReadOnlyList<TocEntry> bodyEntries,
        int frontPageCount,
        ProgramBookCoverDto? cover,
        bool includeTableOfContents)
    {
        using MemoryStream stream = new();
        using PdfWriter writer = new(stream);
        using PdfDocument pdf = new(writer);
        using Document document = new(pdf, PageSize.A4);

        document.SetMargins(38, 42, 38, 42);
        PdfFont regular = CreateFont(bold: false);
        PdfFont bold = CreateFont(bold: true);

        if (cover?.HasImage == true)
        {
            RenderUploadedCover(document, pdf, cover);
        }
        else
        {
            document.Add(new Paragraph(congressName)
                .SetFont(bold)
                .SetFontSize(HeadingFontSize)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(105)
                .SetMarginBottom(12));
            document.Add(new Paragraph("TASLAK PROGRAM KİTABI / DRAFT PROGRAMME BOOK")
                .SetFont(bold)
                .SetFontSize(HeadingFontSize)
                .SetFontColor(ColorConstants.RED)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            if (plan.Days.Count > 0)
            {
                DateOnly first = plan.Days.Min(x => x.Date);
                DateOnly last = plan.Days.Max(x => x.Date);
                string dateText = first == last
                    ? first.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                    : $"{first:dd.MM.yyyy} - {last:dd.MM.yyyy}";
                document.Add(new Paragraph(dateText)
                    .SetFont(regular)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            document.Add(new Paragraph("TASLAK / DRAFT")
                .SetFont(bold)
                .SetFontSize(11)
                .SetFontColor(new DeviceRgb(150, 150, 150))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(80));
        }

        if (includeTableOfContents)
        {
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddBookChapterHeading(
                document,
                "PROGRAM KİTABI / PROGRAMME BOOK",
                "İÇİNDEKİLER / CONTENTS",
                "Bölümler ve sayfa numaraları / Chapters and page numbers",
                regular,
                bold);

            Table toc = new Table(UnitValue.CreatePercentArray(new float[] { 8, 84, 8 }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginTop(14);

            int chapterNumber = 0;
            foreach (TocEntry entry in bodyEntries)
            {
                if (entry.Level == 0)
                    chapterNumber++;

                AddTocEntry(
                    toc,
                    entry,
                    frontPageCount + entry.BodyPageNumber,
                    chapterNumber,
                    regular,
                    bold);
            }

            document.Add(toc);
        }
        document.Close();

        byte[] content = stream.ToArray();
        using PdfDocument pageCounter = new(new PdfReader(new MemoryStream(content)));
        int pageCount = pageCounter.GetNumberOfPages();
        return new FrontRenderResult(content, pageCount);
    }

    private static void RenderUploadedCover(
        Document document,
        PdfDocument pdf,
        ProgramBookCoverDto cover)
    {
        byte[] bytes = cover.ImageBytes
            ?? throw new InvalidOperationException("Kapak görseli içeriği bulunamadı.");

        ImageData imageData;
        try
        {
            imageData = string.Equals(cover.ContentType, "image/png", StringComparison.OrdinalIgnoreCase)
                ? ImageDataFactory.CreatePng(bytes)
                : ImageDataFactory.CreateJpeg(bytes);
        }
        catch (Exception exception) when (exception is iText.IO.Exceptions.IOException
                                          or ArgumentException)
        {
            throw new InvalidOperationException(
                "Kapak görseli PDF içine işlenemedi. Görseli PNG veya JPG olarak yeniden kaydedin.",
                exception);
        }

        PdfPage page = pdf.GetNumberOfPages() == 0
            ? pdf.AddNewPage(PageSize.A4)
            : pdf.GetPage(1);
        Rectangle size = page.GetPageSize();

        float scaledWidth = size.GetWidth();
        float scaledHeight = size.GetHeight();
        float x = 0f;
        float y = 0f;

        PdfCanvas coverCanvas = new(
            page.NewContentStreamAfter(),
            page.GetResources(),
            pdf);
        coverCanvas.AddImageWithTransformationMatrix(
            imageData,
            scaledWidth,
            0,
            0,
            scaledHeight,
            x,
            y,
            false);
    }

    private static byte[] MergeAndNumber(byte[] frontContent, byte[] bodyContent)
    {
        using MemoryStream result = new();
        using PdfWriter writer = new(result);
        using PdfDocument destination = new(writer);
        using PdfDocument front = new(new PdfReader(new MemoryStream(frontContent)));
        using PdfDocument body = new(new PdfReader(new MemoryStream(bodyContent)));

        front.CopyPagesTo(1, front.GetNumberOfPages(), destination);
        body.CopyPagesTo(1, body.GetNumberOfPages(), destination);

        PdfFont pageNumberFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        for (int pageNumber = 1; pageNumber <= destination.GetNumberOfPages(); pageNumber++)
        {
            PdfPage page = destination.GetPage(pageNumber);
            Rectangle size = page.GetPageSize();
            PdfCanvas canvas = new(page.NewContentStreamAfter(), page.GetResources(), destination);
            string text = pageNumber.ToString(CultureInfo.InvariantCulture);
            float textWidth = pageNumberFont.GetWidth(text, 7);
            canvas.BeginText()
                .SetFontAndSize(pageNumberFont, 7)
                .SetFillColor(ColorConstants.GRAY)
                .MoveText((size.GetWidth() - textWidth) / 2f, 12)
                .ShowText(text)
                .EndText();
        }

        destination.Close();
        return result.ToArray();
    }

    private static void AddBoardsPageTitle(Document document, PdfFont bold)
    {
        document.Add(new Paragraph("KONGRE KURULLARI / CONGRESS BOARDS")
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetMarginTop(0)
            .SetMarginBottom(6));
        AddBlueRule(document, marginBottom: 5, height: 1.35f);
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

    private static Cell CreateBoardMemberCompactCell(
        ProgramBoardMemberPdfDto member,
        PdfFont regular,
        PdfFont bold,
        float nameFontSize,
        float institutionFontSize,
        float rowPadding)
    {
        Table line = new Table(UnitValue.CreatePercentArray(new float[] { 44, 56 }))
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMarginTop(0)
            .SetMarginBottom(0);

        line.AddCell(CreateBoardLineCellVeryCompact(member.DisplayName, bold, nameFontSize, rowPadding));
        line.AddCell(CreateBoardLineCellVeryCompact(member.Institution, regular, institutionFontSize, rowPadding));

        return new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPaddingLeft(3)
            .SetPaddingRight(3)
            .SetPaddingTop(0)
            .SetPaddingBottom(0)
            .Add(line);
    }

    private static Cell CreateBoardColumnCell(
        IReadOnlyList<BoardListEntry> entries,
        PdfFont regular,
        PdfFont bold,
        float sectionFontSize,
        float nameFontSize,
        float institutionFontSize,
        float rowPadding)
    {
        Cell cell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPaddingLeft(6)
            .SetPaddingRight(6)
            .SetPaddingTop(0)
            .SetPaddingBottom(0)
            .SetVerticalAlignment(VerticalAlignment.TOP);

        foreach (BoardListEntry entry in entries)
        {
            if (entry.IsSection)
            {
                cell.Add(new Paragraph(entry.Name)
                    .SetFont(bold)
                    .SetFontSize(sectionFontSize)
                    .SetFontColor(new DeviceRgb(36, 74, 145))
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginTop(3)
                    .SetMarginBottom(1));
                continue;
            }

            Table line = new Table(UnitValue.CreatePercentArray(new float[] { 44, 56 }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginTop(0)
                .SetMarginBottom(0);

            line.AddCell(CreateBoardLineCellVeryCompact(entry.Name, bold, nameFontSize, rowPadding));
            line.AddCell(CreateBoardLineCellVeryCompact(entry.Institution, regular, institutionFontSize, rowPadding));
            cell.Add(line);
        }

        return cell;
    }

    private static Cell CreateBoardLineCellVeryCompact(
        string? value,
        PdfFont font,
        float fontSize,
        float rowPadding)
    {
        return new Cell()
            .Add(new Paragraph(value ?? string.Empty)
                .SetFont(font)
                .SetFontSize(fontSize)
                .SetFontColor(new DeviceRgb(20, 28, 45))
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(210, 218, 230), 0.35f))
            .SetPaddingTop(rowPadding)
            .SetPaddingBottom(rowPadding)
            .SetPaddingLeft(2)
            .SetPaddingRight(2)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
    }

    private static Cell CreateParticipantLineCell(
        ProgramParticipantDto participant,
        PdfFont regular,
        PdfFont bold)
    {
        Table nested = new Table(UnitValue.CreatePercentArray(new float[] { 43, 57 }))
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMargin(0);

        nested.AddCell(CreateBoardLineCellVeryCompact(participant.DisplayName, bold, BoardParticipantFontSize, 1.7f));
        nested.AddCell(CreateBoardLineCellVeryCompact(participant.Institution, regular, BoardParticipantFontSize, 1.7f));

        return new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPaddingLeft(5)
            .SetPaddingRight(5)
            .SetPaddingTop(0)
            .SetPaddingBottom(0)
            .Add(nested);
    }

    private static void AddDividerPage(
        Document document,
        PdfDocument pdf,
        string title,
        PdfFont bold)
    {
        if (pdf.GetNumberOfPages() > 0)
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        document.Add(new Paragraph(title)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(330)
            .SetMarginBottom(0));
    }

    private static void AddDayDividerPage(
        Document document,
        PdfDocument pdf,
        ProgramDayDto day,
        PdfFont bold)
    {
        if (pdf.GetNumberOfPages() > 0)
            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

        CultureInfo tr = new("tr-TR");
        CultureInfo en = new("en-US");

        string title = $"{day.Order}. GÜN / DAY {day.Order}";
        string trDate = day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd, dd MMMM yyyy", tr);
        string enDate = day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd, dd MMMM yyyy", en);

        document.Add(new Paragraph(title)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(310)
            .SetMarginBottom(12));

        document.Add(new Paragraph(trDate)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(0)
            .SetMarginBottom(2));

        document.Add(new Paragraph(enDate)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(0)
            .SetMarginBottom(0));
    }

    private static string FormatProgramDayTitle(ProgramDayDto day, CultureInfo cultureInfo)
    {
        string dateText = day.Date.ToDateTime(TimeOnly.MinValue)
            .ToString("dddd, dd MMMM yyyy", cultureInfo);

        return $"{day.Order}. GÜN / DAY {day.Order} - {dateText}";
    }

    private static void AddBlueRule(Document document, float marginBottom = 8, float height = 1.6f)
    {
        Table rule = new Table(1)
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMarginBottom(marginBottom);
        rule.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetBackgroundColor(new DeviceRgb(82, 129, 255))
            .SetHeight(height)
            .SetPadding(0));
        document.Add(rule);
    }

    private static void AddSectionHeading(Document document, string title, PdfFont bold)
    {
        document.Add(new Paragraph(title)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetMarginBottom(12));
    }

    private static void AddBookChapterHeading(
        Document document,
        string overline,
        string title,
        string subtitle,
        PdfFont regular,
        PdfFont bold)
    {
        document.Add(new Paragraph(overline)
            .SetFont(bold)
            .SetFontSize(ProgramFontSize)
            .SetCharacterSpacing(1.1f)
            .SetFontColor(new DeviceRgb(125, 134, 150))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(5));
        document.Add(new Paragraph(title)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(4));
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            document.Add(new Paragraph(subtitle)
                .SetFont(regular)
                .SetFontSize(BoardParticipantFontSize)
                .SetFontColor(new DeviceRgb(90, 99, 115))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(8));
        }

        Table rule = new Table(1)
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMarginBottom(4);
        rule.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetBackgroundColor(new DeviceRgb(36, 74, 145))
            .SetHeight(1.4f)
            .SetPadding(0));
        document.Add(rule);
    }

    private static void AddTocEntry(
        Table toc,
        TocEntry entry,
        int pageNumber,
        int chapterNumber,
        PdfFont regular,
        PdfFont bold)
    {
        Color lineColor = new DeviceRgb(214, 219, 228);
        bool isChapter = entry.Level == 0;

        Cell numberCell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(lineColor, 0.55f))
            .SetPaddingTop(isChapter ? 9 : 6)
            .SetPaddingBottom(isChapter ? 9 : 6)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        if (isChapter)
        {
            numberCell.Add(new Paragraph(chapterNumber.ToString("00", CultureInfo.InvariantCulture))
                .SetFont(bold)
                .SetFontSize(8.5f)
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMargin(0));
            numberCell.SetBackgroundColor(new DeviceRgb(36, 74, 145));
        }
        else
        {
            numberCell.Add(new Paragraph("•")
                .SetFont(bold)
                .SetFontSize(ProgramFontSize)
                .SetFontColor(new DeviceRgb(150, 158, 172))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMargin(0));
        }

        Cell titleCell = new Cell()
            .Add(new Paragraph(entry.Title)
                .SetFont(isChapter ? bold : regular)
                .SetFontSize(isChapter ? 9.4f : 8.1f)
                .SetFontColor(isChapter ? new DeviceRgb(35, 45, 62) : new DeviceRgb(72, 80, 96))
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new DottedBorder(lineColor, 0.65f))
            .SetPaddingLeft(isChapter ? 10 : 20)
            .SetPaddingTop(isChapter ? 9 : 6)
            .SetPaddingBottom(isChapter ? 9 : 6)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);

        Cell pageCell = new Cell()
            .Add(new Paragraph(pageNumber.ToString(CultureInfo.InvariantCulture))
                .SetFont(isChapter ? bold : regular)
                .SetFontSize(isChapter ? 9.4f : 8.1f)
                .SetFontColor(new DeviceRgb(36, 74, 145))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(lineColor, 0.55f))
            .SetPaddingTop(isChapter ? 9 : 6)
            .SetPaddingBottom(isChapter ? 9 : 6)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);

        toc.AddCell(numberCell);
        toc.AddCell(titleCell);
        toc.AddCell(pageCell);
    }

    private static Cell CreateParticipantCell(
        ProgramParticipantDto participant,
        int number,
        PdfFont regular,
        PdfFont bold)
    {
        Table card = new Table(UnitValue.CreatePercentArray(new float[] { 12, 88 }))
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetKeepTogether(true);

        card.AddCell(new Cell()
            .Add(new Paragraph(number.ToString("00", CultureInfo.InvariantCulture))
                .SetFont(bold)
                .SetFontSize(BoardParticipantFontSize)
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBackgroundColor(new DeviceRgb(36, 74, 145))
            .SetPaddingTop(7)
            .SetPaddingBottom(7)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE));

        Cell info = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPaddingLeft(8)
            .SetPaddingRight(4)
            .SetPaddingTop(3)
            .SetPaddingBottom(3)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        info.Add(new Paragraph(participant.DisplayName)
            .SetFont(bold)
            .SetFontSize(BoardParticipantFontSize)
            .SetFontColor(new DeviceRgb(35, 45, 62))
            .SetMargin(0));
        if (!string.IsNullOrWhiteSpace(participant.Institution))
        {
            info.Add(new Paragraph(participant.Institution)
                .SetFont(regular)
                .SetFontSize(BoardParticipantFontSize)
                .SetFontColor(new DeviceRgb(90, 99, 115))
                .SetMarginTop(2)
                .SetMarginBottom(0));
        }
        card.AddCell(info);

        return new Cell()
            .Add(card)
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(226, 230, 237), 0.5f))
            .SetPadding(6)
            .SetVerticalAlignment(VerticalAlignment.TOP);
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

    private static void AddProgrammePageHeader(
        Document document,
        ProgramDayDto day,
        string roomName,
        ProgramSessionDto session,
        PdfFont regular,
        PdfFont bold)
    {
        string dayHeader = BuildProgrammeSessionTopHeader(day, session);

        document.Add(new Paragraph(dayHeader)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(0));

        document.Add(new Paragraph(GetBilingualSessionTitle(session.Title))
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(0)
            .SetMarginBottom(4));

        document.Add(new Paragraph($"{roomName}          {session.StartTime:HH:mm}-{session.EndTime:HH:mm}")
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(190, 0, 0))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(0)
            .SetMarginBottom(4));

        if (!string.IsNullOrWhiteSpace(session.ChairName)
            || !string.IsNullOrWhiteSpace(session.ViceChairName))
        {
            Table officials = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(5);

            officials.AddCell(CreateOfficialCell(
                "Oturum Başkanı / Session Chair",
                session.ChairName,
                regular,
                bold));
            officials.AddCell(CreateOfficialCell(
                "Oturum Başkan Yardımcısı / Vice Chair",
                session.ViceChairName,
                regular,
                bold));
            document.Add(officials);
        }
    }

    private static void AddProgrammeRoomOnlyHeader(
        Document document,
        ProgramDayDto day,
        string roomName,
        PdfFont bold)
    {
        CultureInfo tr = new("tr-TR");
        CultureInfo en = new("en-US");

        string topHeader =
            $"{day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", tr)} / " +
            $"{day.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", en)}";

        document.Add(new Paragraph(topHeader)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(36, 74, 145))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(3));

        document.Add(new Paragraph(roomName)
            .SetFont(bold)
            .SetFontSize(HeadingFontSize)
            .SetFontColor(new DeviceRgb(190, 0, 0))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(0)
            .SetMarginBottom(6));
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

    private static Cell CreateBoardLineCellCompact(
        string? value,
        PdfFont font,
        float fontSize)
    {
        return new Cell()
            .Add(new Paragraph(value ?? string.Empty)
                .SetFont(font)
                .SetFontSize(fontSize)
                .SetFontColor(new DeviceRgb(20, 28, 45))
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(210, 218, 230), 0.4f))
            .SetPaddingTop(2.2f)
            .SetPaddingBottom(2.2f)
            .SetPaddingLeft(4)
            .SetPaddingRight(4)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
    }

        private static Cell CreateBoardLineCell(
        string? value,
        PdfFont font,
        float fontSize)
    {
        return new Cell()
            .Add(new Paragraph(value ?? string.Empty)
                .SetFont(font)
                .SetFontSize(fontSize)
                .SetFontColor(new DeviceRgb(20, 28, 45))
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(210, 218, 230), 0.45f))
            .SetPaddingTop(3)
            .SetPaddingBottom(3)
            .SetPaddingLeft(6)
            .SetPaddingRight(6)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
    }

    private static Cell CreateOfficialCell(
        string label,
        string? value,
        PdfFont regular,
        PdfFont bold)
    {
        Cell cell = new Cell()
            .SetPadding(5)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetBackgroundColor(new DeviceRgb(250, 251, 253));

        cell.Add(new Paragraph(label)
            .SetFont(bold)
            .SetFontSize(BoardParticipantFontSize)
            .SetFontColor(new DeviceRgb(90, 99, 115))
            .SetMargin(0));
        cell.Add(new Paragraph(string.IsNullOrWhiteSpace(value) ? "-" : value)
            .SetFont(regular)
            .SetFontSize(ProgramFontSize)
            .SetMarginTop(2)
            .SetMarginBottom(0));
        return cell;
    }

    private static Cell CreateAuthorsCell(
        string? authors,
        PdfFont font,
        float fontSize)
    {
        Cell cell = new Cell()
            .SetPadding(5)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);

        IReadOnlyList<string> authorNames = SplitAuthors(authors);
        cell.Add(new Paragraph(string.Join(" - ", authorNames))
            .SetFont(font)
            .SetFontSize(fontSize)
            .SetMargin(0));

        return cell;
    }

    private static IReadOnlyList<string> SplitAuthors(string? authors)
    {
        if (string.IsNullOrWhiteSpace(authors))
            return new[] { string.Empty };

        return authors
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split(new[] { "\n", " - " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static Cell CreateCell(
        string? value,
        PdfFont font,
        float fontSize,
        Color? backgroundColor = null)
    {
        Cell cell = new Cell()
            .Add(new Paragraph(value ?? string.Empty)
                .SetFont(font)
                .SetFontSize(fontSize)
                .SetMargin(0))
            .SetPadding(5)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);

        if (backgroundColor is not null)
            cell.SetBackgroundColor(backgroundColor);

        return cell;
    }

    private static PdfFont CreateFont(bool bold)
    {
        foreach (string path in BuildFontCandidates(bold))
        {
            if (!File.Exists(path))
                continue;

            try
            {
                return PdfFontFactory.CreateFont(
                    path,
                    PdfEncodings.IDENTITY_H,
                    PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            }
            catch
            {
                // Try the next candidate.
            }
        }

        return PdfFontFactory.CreateFont(bold ? StandardFonts.TIMES_BOLD : StandardFonts.TIMES_ROMAN);
    }

    private static IEnumerable<string> BuildFontCandidates(bool bold)
    {
        string suffix = bold ? "Bold" : "Regular";
        string? configured = Environment.GetEnvironmentVariable($"SYMPLIFY_PROGRAM_PDF_FONT_{suffix.ToUpperInvariant()}");
        if (!string.IsNullOrWhiteSpace(configured))
            yield return configured.Trim();

        string timesFileName = bold ? "TimesNewRoman-Bold.ttf" : "TimesNewRoman.ttf";
        yield return IOPath.Combine(AppContext.BaseDirectory, "Templates", "Fonts", timesFileName);
        yield return IOPath.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", timesFileName);

        string liberationFileName = bold ? "LiberationSerif-Bold.ttf" : "LiberationSerif-Regular.ttf";
        yield return IOPath.Combine(AppContext.BaseDirectory, "Templates", "Fonts", liberationFileName);
        yield return IOPath.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", liberationFileName);
        yield return bold
            ? "/usr/share/fonts/truetype/liberation/LiberationSerif-Bold.ttf"
            : "/usr/share/fonts/truetype/liberation/LiberationSerif-Regular.ttf";

        string fileName = bold ? "DejaVuSans-Bold.ttf" : "DejaVuSans.ttf";
        yield return IOPath.Combine(AppContext.BaseDirectory, "Templates", "Fonts", fileName);
        yield return IOPath.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", fileName);
        yield return bold
            ? "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
            : "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        yield return bold
            ? @"C:\Windows\Fonts\arialbd.ttf"
            : @"C:\Windows\Fonts\arial.ttf";
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

    private sealed record BoardListEntry(bool IsSection, string Name, string Institution)
    {
        public static BoardListEntry Section(string name) => new(true, name, string.Empty);
        public static BoardListEntry Member(string name, string? institution) => new(false, name, institution ?? string.Empty);
    }

    private sealed record TocEntry(string Title, int BodyPageNumber, int Level);
    private sealed record BodyRenderResult(byte[] Content, IReadOnlyList<TocEntry> TocEntries);
    private sealed record FrontRenderResult(byte[] Content, int PageCount);
}
