using System.Globalization;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using IOPath = System.IO.Path;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Services;

public sealed class AbstractBookPdfRenderer : IAbstractBookPdfRenderer
{
    public byte[] Render(AbstractBookDocumentModel model, string? culture)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Entries.Count == 0)
            throw new InvalidOperationException("Özet kitabı için bildiri bulunamadı.");

        CultureInfo cultureInfo = ResolveCulture(culture);
        ThemePalette palette = ResolvePalette(model.Options.CoverTheme);
        BodyRenderResult body = RenderBody(model, cultureInfo, palette);

        bool hasFrontMatter = model.Options.IncludeCover
                              || model.Options.IncludeTableOfContents
                              || (model.Options.IncludeBoards && model.Boards.Count > 0);

        if (!hasFrontMatter)
            return AddPageNumbersAndHeaders(body.Content, model, 0, palette);

        int expectedFrontPageCount = 1;
        FrontRenderResult front = RenderFront(model, body.TocEntries, expectedFrontPageCount, palette);
        for (int attempt = 0; attempt < 3 && front.PageCount != expectedFrontPageCount; attempt++)
        {
            expectedFrontPageCount = front.PageCount;
            front = RenderFront(model, body.TocEntries, expectedFrontPageCount, palette);
        }

        return MergeAndDecorate(front.Content, body.Content, model, front.PageCount, palette);
    }

    private static BodyRenderResult RenderBody(
        AbstractBookDocumentModel model,
        CultureInfo cultureInfo,
        ThemePalette palette)
    {
        using MemoryStream stream = new();
        using PdfWriter writer = new(stream);
        using PdfDocument pdf = new(writer);
        using Document document = new(pdf, PageSize.A4);

        document.SetMargins(44, 46, 42, 46);
        PdfFont regular = CreateFont(false);
        PdfFont bold = CreateFont(true);
        PdfFont italic = CreateFont(false, true);
        List<TocEntry> tocEntries = new();

        for (int index = 0; index < model.Entries.Count; index++)
        {
            AbstractBookEntryDto entry = model.Entries[index];
            if (index > 0 && model.Options.StartEachSubmissionOnNewPage)
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            if (pdf.GetNumberOfPages() == 0)
                pdf.AddNewPage(PageSize.A4);

            string displayTitle = model.Options.IncludeTurkishContent
                ? FirstNonEmpty(entry.TurkishTitle, entry.EnglishTitle, entry.SubmissionNumber)
                : FirstNonEmpty(entry.EnglishTitle, entry.TurkishTitle, entry.SubmissionNumber);
            tocEntries.Add(new TocEntry(
                $"{entry.SubmissionNumber} - {displayTitle}",
                pdf.GetNumberOfPages()));

            RenderSubmission(document, entry, model, regular, bold, italic, palette, cultureInfo);

            if (index + 1 < model.Entries.Count && !model.Options.StartEachSubmissionOnNewPage)
            {
                document.Add(new Paragraph(" ")
                    .SetBorderBottom(new SolidBorder(palette.Border, 0.6f))
                    .SetMarginTop(10)
                    .SetMarginBottom(10));
            }
        }

        document.Close();
        return new BodyRenderResult(stream.ToArray(), tocEntries);
    }

    private static void RenderSubmission(
        Document document,
        AbstractBookEntryDto entry,
        AbstractBookDocumentModel model,
        PdfFont regular,
        PdfFont bold,
        PdfFont italic,
        ThemePalette palette,
        CultureInfo cultureInfo)
    {
        AbstractBookOptionsDto options = model.Options;

        document.Add(CreateSubmissionBanner(model, regular, bold, palette));

        Table meta = new Table(UnitValue.CreatePercentArray(new float[] { 48, 26, 26 }))
            .UseAllAvailableWidth()
            .SetMarginBottom(4)
            .SetBorder(Border.NO_BORDER);

        meta.AddCell(CreateMetaCell("BİLDİRİ NO / SUBMISSION NO", entry.SubmissionNumber, regular, bold, palette, TextAlignment.LEFT));
        meta.AddCell(CreateMetaCell("TÜR / TYPE", entry.SubmissionTypeName, regular, bold, palette, TextAlignment.CENTER));
        meta.AddCell(CreateMetaCell("KONU / TOPIC", entry.TopicName, regular, bold, palette, TextAlignment.RIGHT));
        document.Add(meta);
        document.Add(CreateSubmissionOrcidParagraph(entry.Authors, options.IncludeOrcid, bold, palette));

        if (options.IncludeTurkishContent && !string.IsNullOrWhiteSpace(entry.TurkishTitle))
        {
            document.Add(new Paragraph(entry.TurkishTitle)
                .SetFont(bold)
                .SetFontSize(15)
                .SetFontColor(palette.Primary)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMultipliedLeading(1.18f)
                .SetMarginBottom(8));
        }

        RenderAuthors(document, entry.Authors, options, regular, bold, palette);

        bool renderedContent = false;
        if (options.IncludeTurkishContent && !string.IsNullOrWhiteSpace(entry.TurkishAbstract))
        {
            AddContentSection(
                document,
                "ÖZET",
                entry.TurkishAbstract,
                "Anahtar Kelimeler",
                entry.TurkishKeywords,
                regular,
                bold,
                palette);
            renderedContent = true;
        }

        if (options.IncludeEnglishContent && !string.IsNullOrWhiteSpace(entry.EnglishTitle))
        {
            bool hasTurkishHeading = options.IncludeTurkishContent
                                     && !string.IsNullOrWhiteSpace(entry.TurkishTitle);

            document.Add(new Paragraph(entry.EnglishTitle)
                .SetFont(italic)
                .SetFontSize(hasTurkishHeading ? 10.5f : 15f)
                .SetFontColor(hasTurkishHeading ? palette.Muted : palette.Primary)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMultipliedLeading(1.15f)
                .SetMarginTop(renderedContent ? 6 : 10)
                .SetMarginBottom(6));
        }

        if (options.IncludeEnglishContent && !string.IsNullOrWhiteSpace(entry.EnglishAbstract))
        {
            AddContentSection(
                document,
                "ABSTRACT",
                entry.EnglishAbstract,
                "Keywords",
                entry.EnglishKeywords,
                regular,
                bold,
                palette);
            renderedContent = true;
        }

        if (!renderedContent)
        {
            document.Add(new Paragraph("Bu bildiri için seçilen dilde özet içeriği bulunamadı. / No abstract content is available in the selected language.")
                .SetFont(regular)
                .SetFontSize(9)
                .SetFontColor(ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(18));
        }

    }

    private static Paragraph CreateSubmissionOrcidParagraph(
        IReadOnlyList<AbstractBookAuthorDto> authors,
        bool includeOrcid,
        PdfFont bold,
        ThemePalette palette)
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

        return new Paragraph(text)
            .SetFont(bold)
            .SetFontSize(10.5f)
            .SetFontColor(palette.Muted)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(4)
            .SetMarginBottom(8)
            .SetMultipliedLeading(1.1f);
    }

    private static void RenderAuthors(
        Document document,
        IReadOnlyList<AbstractBookAuthorDto> authors,
        AbstractBookOptionsDto options,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        if (authors.Count == 0)
            return;

        List<string> institutions = authors
            .Select(x => x.Institution?.Trim() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Paragraph authorLine = new Paragraph()
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(4)
            .SetMarginBottom(5)
            .SetMultipliedLeading(1.15f);

        for (int index = 0; index < authors.Count; index++)
        {
            AbstractBookAuthorDto author = authors[index];
            if (index > 0)
                authorLine.Add(new Text(", ").SetFont(regular).SetFontSize(9.3f));

            string correspondingMarker = options.IncludeCorrespondingAuthor && author.IsCorrespondingAuthor ? "*" : string.Empty;
            authorLine.Add(new Text(author.DisplayName + correspondingMarker)
                .SetFont(bold)
                .SetFontSize(9.3f)
                .SetFontColor(new DeviceRgb(45, 50, 60)));

            if (options.IncludeInstitutions && !string.IsNullOrWhiteSpace(author.Institution))
            {
                int institutionIndex = institutions.FindIndex(x => string.Equals(
                    x,
                    author.Institution,
                    StringComparison.CurrentCultureIgnoreCase));
                if (institutionIndex >= 0)
                {
                    authorLine.Add(new Text((institutionIndex + 1).ToString(CultureInfo.InvariantCulture))
                        .SetFont(regular)
                        .SetFontSize(6.6f)
                        .SetTextRise(4));
                }
            }
        }

        document.Add(authorLine);

        if (options.IncludeInstitutions)
        {
            for (int index = 0; index < institutions.Count; index++)
            {
                document.Add(new Paragraph($"{index + 1} {institutions[index]}")
                    .SetFont(regular)
                    .SetFontSize(7.8f)
                    .SetFontColor(palette.Muted)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMargin(0));
            }
        }

        if (options.IncludeCorrespondingAuthor)
        {
            IReadOnlyList<AbstractBookAuthorDto> correspondingAuthors = authors
                .Where(author => author.IsCorrespondingAuthor)
                .ToList();

            foreach (AbstractBookAuthorDto corresponding in correspondingAuthors)
            {
                string authorName = FirstNonEmpty(corresponding.PlainName, corresponding.DisplayName);

                document.Add(new Paragraph($"* Corresponding Author: {authorName}")
                    .SetFont(regular)
                    .SetFontSize(7.4f)
                    .SetFontColor(palette.Muted)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(4)
                    .SetMarginBottom(0));

                if (!string.IsNullOrWhiteSpace(corresponding.Email))
                {
                    document.Add(new Paragraph(corresponding.Email.Trim())
                        .SetFont(regular)
                        .SetFontSize(7.2f)
                        .SetFontColor(palette.Muted)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(0)
                        .SetMarginBottom(0));
                }
            }
        }

        document.Add(new Paragraph()
            .SetMarginTop(0)
            .SetMarginBottom(5));
    }

    private static void AddContentSection(
        Document document,
        string heading,
        string content,
        string keywordLabel,
        string keywords,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        Table headingTable = new Table(1)
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMarginTop(10)
            .SetMarginBottom(7);
        headingTable.AddCell(new Cell()
            .Add(new Paragraph(heading)
                .SetFont(bold)
                .SetFontSize(9.5f)
                .SetFontColor(ColorConstants.WHITE)
                .SetMargin(0))
            .SetBackgroundColor(palette.Primary)
            .SetBorder(Border.NO_BORDER)
            .SetPaddingTop(5)
            .SetPaddingBottom(5)
            .SetPaddingLeft(8));
        document.Add(headingTable);

        foreach (string paragraphText in SplitParagraphs(content))
        {
            document.Add(new Paragraph(paragraphText)
                .SetFont(regular)
                .SetFontSize(8.8f)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetMultipliedLeading(1.28f)
                .SetMarginTop(0)
                .SetMarginBottom(5));
        }

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            Paragraph keywordParagraph = new Paragraph()
                .SetTextAlignment(TextAlignment.LEFT)
                .SetMarginTop(5)
                .SetMarginBottom(4);
            keywordParagraph.Add(new Text($"{keywordLabel}: ")
                .SetFont(bold)
                .SetFontSize(8.2f)
                .SetFontColor(palette.Primary));
            keywordParagraph.Add(new Text(keywords)
                .SetFont(regular)
                .SetFontSize(8.2f)
                .SetFontColor(new DeviceRgb(60, 66, 76)));
            document.Add(keywordParagraph);
        }
    }

    private static IEnumerable<string> SplitParagraphs(string content)
        => content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);


    private static IBlockElement CreateSubmissionBanner(
        AbstractBookDocumentModel model,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        string congressName = FirstNonEmpty(model.CongressEnglishName, model.CongressName);
        string dateText = FormatHeaderDateRange(model.StartDate, model.EndDate);
        string locationText = BuildHeaderLocation(model.City, model.Venue);
        bool hasLogo = model.Options.HeaderLogoBytes is { Length: > 0 };

        Table table = new Table(UnitValue.CreatePercentArray(new float[] { 14, 72, 14 }))
            .UseAllAvailableWidth()
            .SetMarginBottom(10)
            .SetBorder(Border.NO_BORDER);

        table.AddCell(CreateBannerLogoCell(model.Options.HeaderLogoBytes, TextAlignment.LEFT));

        Paragraph center = new Paragraph()
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMargin(0)
            .SetMultipliedLeading(1.05f);

        center.Add(new Text(congressName)
            .SetFont(bold)
            .SetFontSize(8.5f)
            .SetFontColor(palette.Primary));

        if (!string.IsNullOrWhiteSpace(dateText))
        {
            center.Add("\n");
            center.Add(new Text(dateText)
                .SetFont(regular)
                .SetFontSize(7.2f)
                .SetFontColor(palette.Muted));
        }

        if (!string.IsNullOrWhiteSpace(locationText))
        {
            center.Add("\n");
            center.Add(new Text(locationText)
                .SetFont(regular)
                .SetFontSize(7.2f)
                .SetFontColor(palette.Muted));
        }

        table.AddCell(new Cell()
            .Add(center)
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE));

        table.AddCell(CreateBannerLogoCell(model.Options.HeaderLogoBytes, TextAlignment.RIGHT));

        if (hasLogo)
            return table;

        return center.SetMarginBottom(10);
    }

    private static Cell CreateBannerLogoCell(byte[]? logoBytes, TextAlignment alignment)
    {
        Cell cell = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);

        if (logoBytes is not { Length: > 0 })
            return cell;

        Image image = CreateDocumentImage(logoBytes, "Kongre logosu");
        image.ScaleToFit(42, 42);
        image.SetHorizontalAlignment(alignment == TextAlignment.RIGHT
            ? HorizontalAlignment.RIGHT
            : HorizontalAlignment.LEFT);
        cell.Add(image);
        return cell;
    }

    private static Image CreateDocumentImage(byte[] bytes, string label)
        => new(CreateDocumentImageData(bytes, label));

    private static ImageData CreateDocumentImageData(byte[] bytes, string label)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        try
        {
            if (IsPng(bytes))
                return ImageDataFactory.CreatePng(bytes);

            if (IsJpeg(bytes))
                return ImageDataFactory.CreateJpeg(bytes);

            throw new InvalidOperationException(
                $"{label} desteklenen bir PNG veya JPG dosyası değil.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is iText.IO.Exceptions.IOException
                                          or ArgumentException)
        {
            throw new InvalidOperationException(
                $"{label} okunamadı. Dosya uzantısı doğru görünse bile görsel içeriği bozuk veya desteklenmeyen bir PNG/JPG varyantı olabilir.",
                exception);
        }
    }

    private static bool IsPng(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 8
               && bytes[0] == 0x89
               && bytes[1] == 0x50
               && bytes[2] == 0x4E
               && bytes[3] == 0x47
               && bytes[4] == 0x0D
               && bytes[5] == 0x0A
               && bytes[6] == 0x1A
               && bytes[7] == 0x0A;
    }

    private static bool IsJpeg(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 3
               && bytes[0] == 0xFF
               && bytes[1] == 0xD8
               && bytes[2] == 0xFF;
    }

    private static Cell CreateMetaCell(
        string label,
        string value,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette,
        TextAlignment alignment)
    {
        Paragraph paragraph = new Paragraph()
            .SetTextAlignment(alignment)
            .SetMargin(0);
        paragraph.Add(new Text(label + "\n")
            .SetFont(bold)
            .SetFontSize(6.4f)
            .SetFontColor(palette.Muted));
        paragraph.Add(new Text(string.IsNullOrWhiteSpace(value) ? "-" : value)
            .SetFont(regular)
            .SetFontSize(8)
            .SetFontColor(new DeviceRgb(35, 42, 54)));

        return new Cell()
            .Add(paragraph)
            .SetBackgroundColor(palette.SoftBackground)
            .SetBorder(Border.NO_BORDER)
            .SetPadding(7);
    }

    private static FrontRenderResult RenderFront(
        AbstractBookDocumentModel model,
        IReadOnlyList<TocEntry> bodyEntries,
        int frontPageCount,
        ThemePalette palette)
    {
        using MemoryStream stream = new();
        using PdfWriter writer = new(stream);
        using PdfDocument pdf = new(writer);
        using Document document = new(pdf, PageSize.A4);

        document.SetMargins(42, 46, 42, 46);
        PdfFont regular = CreateFont(false);
        PdfFont bold = CreateFont(true);
        bool hasPreviousSection = false;

        if (model.Options.IncludeCover)
        {
            RenderCover(document, pdf, model, regular, bold, palette);
            hasPreviousSection = true;
        }

        // Ön bölüm sırası sabittir: Kapak -> İçindekiler -> Kurullar.
        // Yayın künyesi ekran ve çıktı akışından kaldırılmıştır.
        if (model.Options.IncludeTableOfContents)
        {
            if (hasPreviousSection)
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            RenderTableOfContents(document, bodyEntries, frontPageCount, regular, bold, palette);
            hasPreviousSection = true;
        }

        if (model.Options.IncludeBoards && model.Boards.Count > 0)
        {
            if (hasPreviousSection)
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            RenderBoards(document, model.Boards, regular, bold, palette);
            hasPreviousSection = true;
        }

        document.Close();
        byte[] content = stream.ToArray();
        using PdfDocument counter = new(new PdfReader(new MemoryStream(content)));
        return new FrontRenderResult(content, counter.GetNumberOfPages());
    }

    private static void RenderCover(
        Document document,
        PdfDocument pdf,
        AbstractBookDocumentModel model,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        PdfPage page = pdf.GetNumberOfPages() == 0
            ? pdf.AddNewPage(PageSize.A4)
            : pdf.GetPage(1);
        Rectangle size = page.GetPageSize();

        if (model.Options.CoverImageBytes is { Length: > 0 })
        {
            ImageData coverImageData = CreateDocumentImageData(
                model.Options.CoverImageBytes,
                "Kapak görseli");
            float sourceWidth = coverImageData.GetWidth();
            float sourceHeight = coverImageData.GetHeight();

            if (sourceWidth <= 0 || sourceHeight <= 0)
                throw new InvalidOperationException("Kapak görselinin boyutları okunamadı.");

            // Kapak, layout motoruna bırakılmadan doğrudan ilk PDF sayfasının
            // content stream'ine çizilir. Böylece margin/fixed-position/flush
            // davranışlarından etkilenmez ve birleşmiş PDF'de kesin olarak korunur.
            float scale = Math.Max(size.GetWidth() / sourceWidth, size.GetHeight() / sourceHeight);
            float scaledWidth = sourceWidth * scale;
            float scaledHeight = sourceHeight * scale;
            float x = (size.GetWidth() - scaledWidth) / 2f;
            float y = (size.GetHeight() - scaledHeight) / 2f;

            PdfCanvas coverCanvas = new(page.NewContentStreamAfter(), page.GetResources(), pdf);
            coverCanvas.AddImageWithTransformationMatrix(
                coverImageData,
                scaledWidth,
                0,
                0,
                scaledHeight,
                x,
                y,
                false);
            return;
        }

        PdfCanvas canvas = new(page.NewContentStreamBefore(), page.GetResources(), pdf);
        canvas.SaveState()
            .SetFillColor(palette.CoverBackground)
            .Rectangle(0, 0, size.GetWidth(), size.GetHeight())
            .Fill()
            .SetFillColor(palette.Accent)
            .Rectangle(0, 0, 18, size.GetHeight())
            .Fill()
            .RestoreState();

        document.Add(new Paragraph(model.CongressCode)
            .SetFont(bold)
            .SetFontSize(10)
            .SetFontColor(palette.CoverMuted)
            .SetCharacterSpacing(1.5f)
            .SetMarginTop(70)
            .SetMarginLeft(18)
            .SetMarginBottom(18));

        document.Add(new Paragraph(model.CongressName)
            .SetFont(bold)
            .SetFontSize(24)
            .SetFontColor(palette.CoverText)
            .SetMultipliedLeading(1.14f)
            .SetMarginLeft(18)
            .SetMarginRight(18)
            .SetMarginBottom(12));

        if (!string.IsNullOrWhiteSpace(model.CongressSubtitle))
        {
            document.Add(new Paragraph(model.CongressSubtitle)
                .SetFont(regular)
                .SetFontSize(12)
                .SetFontColor(palette.CoverMuted)
                .SetMarginLeft(18)
                .SetMarginRight(18)
                .SetMarginBottom(28));
        }

        document.Add(new Paragraph(FirstNonEmpty(model.Options.EnglishBookTitle, "ABSTRACT BOOK").ToUpperInvariant())
            .SetFont(bold)
            .SetFontSize(14)
            .SetFontColor(palette.Accent)
            .SetCharacterSpacing(1.2f)
            .SetMarginLeft(18)
            .SetMarginBottom(5));
        document.Add(new Paragraph(FirstNonEmpty(model.Options.BookTitle, "Özet Kitabı"))
            .SetFont(bold)
            .SetFontSize(31)
            .SetFontColor(palette.CoverText)
            .SetMarginLeft(18)
            .SetMarginBottom(36));

        string dateText = FormatDateRange(model.StartDate, model.EndDate);
        string location = string.Join(" - ", new[] { model.City, model.Venue }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        document.Add(new Paragraph(string.Join("\n", new[] { dateText, location }
                .Where(x => !string.IsNullOrWhiteSpace(x))))
            .SetFont(regular)
            .SetFontSize(11)
            .SetFontColor(palette.CoverMuted)
            .SetMarginLeft(18)
            .SetMultipliedLeading(1.3f));

        if (!string.IsNullOrWhiteSpace(model.Options.Editor))
        {
            document.Add(new Paragraph($"Editör / Editor\n{model.Options.Editor}")
                .SetFont(regular)
                .SetFontSize(9.5f)
                .SetFontColor(palette.CoverMuted)
                .SetMarginTop(60)
                .SetMarginLeft(18));
        }

        if (!string.IsNullOrWhiteSpace(model.Options.Isbn))
        {
            document.Add(new Paragraph($"ISBN: {model.Options.Isbn}")
                .SetFont(regular)
                .SetFontSize(8.5f)
                .SetFontColor(palette.CoverMuted)
                .SetMarginTop(12)
                .SetMarginLeft(18));
        }
    }

    private static void RenderPublicationInfo(
        Document document,
        AbstractBookDocumentModel model,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        AddChapterHeading(document, "YAYIN KÜNYESİ / PUBLICATION INFORMATION", null, regular, bold, palette);

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

        Table table = new Table(UnitValue.CreatePercentArray(new float[] { 38, 62 }))
            .UseAllAvailableWidth()
            .SetMarginTop(18)
            .SetBorder(Border.NO_BORDER);

        List<(string Label, string Value)> populatedRows = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToList();

        foreach ((string label, string value) in populatedRows)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(label).SetFont(bold).SetFontSize(8.5f).SetMargin(0))
                .SetBackgroundColor(palette.SoftBackground)
                .SetBorder(new SolidBorder(palette.Border, 0.5f))
                .SetPadding(7));
            table.AddCell(new Cell()
                .Add(new Paragraph(value).SetFont(regular).SetFontSize(8.5f).SetMargin(0))
                .SetBorder(new SolidBorder(palette.Border, 0.5f))
                .SetPadding(7));
        }

        if (populatedRows.Count == 0)
        {
            document.Add(new Paragraph("Yayın künyesi bilgisi girilmedi. / No publication information was entered.")
                .SetFont(regular)
                .SetFontSize(9)
                .SetFontColor(palette.Muted)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(24));
        }
        else
        {
            document.Add(table);
        }
    }

    private static void RenderBoards(
        Document document,
        IReadOnlyList<ProgramBoardSectionDto> boards,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        AddChapterHeading(document, "KONGRE KURULLARI / CONGRESS BOARDS", null, regular, bold, palette);

        foreach (ProgramBoardSectionDto board in boards
                     .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                     .ThenBy(x => x.Name))
        {
            document.Add(new Paragraph(board.Name)
                .SetFont(bold)
                .SetFontSize(11)
                .SetFontColor(palette.Primary)
                .SetMarginTop(14)
                .SetMarginBottom(6));

            Table table = new Table(UnitValue.CreatePercentArray(new float[] { 40, 60 }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER);

            foreach (ProgramBoardMemberPdfDto member in board.Members
                         .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                         .ThenBy(x => x.DisplayName))
            {
                table.AddCell(new Cell()
                    .Add(new Paragraph(member.DisplayName).SetFont(bold).SetFontSize(8).SetMargin(0))
                    .SetBorderBottom(new SolidBorder(palette.Border, 0.45f))
                    .SetBorderTop(Border.NO_BORDER)
                    .SetBorderLeft(Border.NO_BORDER)
                    .SetBorderRight(Border.NO_BORDER)
                    .SetPadding(5));
                table.AddCell(new Cell()
                    .Add(new Paragraph(member.Institution).SetFont(regular).SetFontSize(8).SetMargin(0))
                    .SetBorderBottom(new SolidBorder(palette.Border, 0.45f))
                    .SetBorderTop(Border.NO_BORDER)
                    .SetBorderLeft(Border.NO_BORDER)
                    .SetBorderRight(Border.NO_BORDER)
                    .SetPadding(5));
            }

            document.Add(table);
        }
    }

    private static void RenderTableOfContents(
        Document document,
        IReadOnlyList<TocEntry> entries,
        int frontPageCount,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        AddChapterHeading(
            document,
            "İÇİNDEKİLER / CONTENTS",
            $"{entries.Count} bildiri / submissions",
            regular,
            bold,
            palette);

        Table toc = new Table(UnitValue.CreatePercentArray(new float[] { 8, 84, 8 }))
            .UseAllAvailableWidth()
            .SetBorder(Border.NO_BORDER)
            .SetMarginTop(14);

        for (int index = 0; index < entries.Count; index++)
        {
            TocEntry entry = entries[index];
            AddTocEntry(toc, index + 1, entry.Title, frontPageCount + entry.BodyPageNumber, regular, bold, palette);
        }

        document.Add(toc);
    }

    private static void AddChapterHeading(
        Document document,
        string title,
        string? subtitle,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        document.Add(new Paragraph(title)
            .SetFont(bold)
            .SetFontSize(20)
            .SetFontColor(palette.Primary)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(8)
            .SetMarginBottom(4));

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            document.Add(new Paragraph(subtitle)
                .SetFont(regular)
                .SetFontSize(8.5f)
                .SetFontColor(palette.Muted)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(8));
        }

        Table rule = new Table(1).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
        rule.AddCell(new Cell()
            .SetHeight(1.5f)
            .SetPadding(0)
            .SetBorder(Border.NO_BORDER)
            .SetBackgroundColor(palette.Accent));
        document.Add(rule);
    }

    private static void AddTocEntry(
        Table toc,
        int number,
        string title,
        int pageNumber,
        PdfFont regular,
        PdfFont bold,
        ThemePalette palette)
    {
        Color border = palette.Border;
        toc.AddCell(new Cell()
            .Add(new Paragraph(number.ToString("00", CultureInfo.InvariantCulture))
                .SetFont(bold)
                .SetFontSize(7.8f)
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMargin(0))
            .SetBackgroundColor(palette.Primary)
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(border, 0.5f))
            .SetPaddingTop(6)
            .SetPaddingBottom(6));

        toc.AddCell(new Cell()
            .Add(new Paragraph(title)
                .SetFont(regular)
                .SetFontSize(7.7f)
                .SetFontColor(new DeviceRgb(55, 61, 72))
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new DottedBorder(border, 0.55f))
            .SetPaddingTop(6)
            .SetPaddingBottom(6)
            .SetPaddingLeft(9));

        toc.AddCell(new Cell()
            .Add(new Paragraph(pageNumber.ToString(CultureInfo.InvariantCulture))
                .SetFont(bold)
                .SetFontSize(8)
                .SetFontColor(palette.Primary)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMargin(0))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(border, 0.5f))
            .SetPaddingTop(6)
            .SetPaddingBottom(6));
    }

    private static byte[] MergeAndDecorate(
        byte[] frontContent,
        byte[] bodyContent,
        AbstractBookDocumentModel model,
        int frontPageCount,
        ThemePalette palette)
    {
        using MemoryStream result = new();
        using PdfWriter writer = new(result);
        using PdfDocument destination = new(writer);
        using PdfDocument front = new(new PdfReader(new MemoryStream(frontContent)));
        using PdfDocument body = new(new PdfReader(new MemoryStream(bodyContent)));

        front.CopyPagesTo(1, front.GetNumberOfPages(), destination);
        body.CopyPagesTo(1, body.GetNumberOfPages(), destination);
        DecoratePages(destination, model, frontPageCount, palette);
        destination.Close();
        return result.ToArray();
    }

    private static byte[] AddPageNumbersAndHeaders(
        byte[] content,
        AbstractBookDocumentModel model,
        int frontPageCount,
        ThemePalette palette)
    {
        using MemoryStream result = new();
        using PdfDocument source = new(new PdfReader(new MemoryStream(content)));
        using PdfWriter writer = new(result);
        using PdfDocument destination = new(writer);
        source.CopyPagesTo(1, source.GetNumberOfPages(), destination);
        DecoratePages(destination, model, frontPageCount, palette);
        destination.Close();
        return result.ToArray();
    }

    private static void DecoratePages(
        PdfDocument pdf,
        AbstractBookDocumentModel model,
        int frontPageCount,
        ThemePalette palette)
    {
        PdfFont regular = CreateFont(false);

        for (int pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
        {
            PdfPage page = pdf.GetPage(pageNumber);
            Rectangle size = page.GetPageSize();
            PdfCanvas canvas = new(page.NewContentStreamAfter(), page.GetResources(), pdf);

            string numberText = pageNumber.ToString(CultureInfo.InvariantCulture);
            float width = regular.GetWidth(numberText, 7);
            canvas.BeginText()
                .SetFontAndSize(regular, 7)
                .SetFillColor(palette.Muted)
                .MoveText((size.GetWidth() - width) / 2f, 13)
                .ShowText(numberText)
                .EndText();
        }
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

    private static ThemePalette ResolvePalette(AbstractBookCoverTheme theme)
    {
        return theme switch
        {
            AbstractBookCoverTheme.Minimal => new ThemePalette(
                new DeviceRgb(30, 32, 36),
                new DeviceRgb(95, 99, 108),
                new DeviceRgb(230, 232, 236),
                new DeviceRgb(246, 246, 247),
                new DeviceRgb(250, 250, 250),
                new DeviceRgb(30, 32, 36),
                new DeviceRgb(90, 94, 102),
                new DeviceRgb(30, 32, 36)),
            AbstractBookCoverTheme.Editorial => new ThemePalette(
                new DeviceRgb(18, 55, 70),
                new DeviceRgb(60, 103, 117),
                new DeviceRgb(196, 218, 218),
                new DeviceRgb(237, 246, 245),
                new DeviceRgb(11, 42, 55),
                ColorConstants.WHITE,
                new DeviceRgb(205, 225, 226),
                new DeviceRgb(76, 206, 180)),
            _ => new ThemePalette(
                new DeviceRgb(36, 74, 145),
                new DeviceRgb(90, 99, 115),
                new DeviceRgb(208, 216, 230),
                new DeviceRgb(240, 244, 252),
                new DeviceRgb(25, 48, 96),
                ColorConstants.WHITE,
                new DeviceRgb(210, 221, 244),
                new DeviceRgb(79, 140, 255))
        };
    }

    private static PdfFont CreateFont(bool bold, bool italic = false)
    {
        foreach (string path in BuildFontCandidates(bold, italic))
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
            }
        }

        string fallback = bold
            ? StandardFonts.HELVETICA_BOLD
            : italic
                ? StandardFonts.HELVETICA_OBLIQUE
                : StandardFonts.HELVETICA;
        return PdfFontFactory.CreateFont(fallback);
    }

    private static IEnumerable<string> BuildFontCandidates(bool bold, bool italic)
    {
        string suffix = bold ? "BOLD" : italic ? "ITALIC" : "REGULAR";
        string? configured = Environment.GetEnvironmentVariable($"SYMPLIFY_ABSTRACT_BOOK_PDF_FONT_{suffix}");
        if (!string.IsNullOrWhiteSpace(configured))
            yield return configured.Trim();

        string fileName = bold
            ? "DejaVuSans-Bold.ttf"
            : italic
                ? "DejaVuSans-Oblique.ttf"
                : "DejaVuSans.ttf";
        yield return IOPath.Combine(AppContext.BaseDirectory, "Templates", "Fonts", fileName);
        yield return IOPath.Combine(Directory.GetCurrentDirectory(), "Templates", "Fonts", fileName);
        yield return bold
            ? "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
            : italic
                ? "/usr/share/fonts/truetype/dejavu/DejaVuSans-Oblique.ttf"
                : "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        yield return bold
            ? @"C:\Windows\Fonts\arialbd.ttf"
            : italic
                ? @"C:\Windows\Fonts\ariali.ttf"
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

    private sealed record TocEntry(string Title, int BodyPageNumber);
    private sealed record BodyRenderResult(byte[] Content, IReadOnlyList<TocEntry> TocEntries);
    private sealed record FrontRenderResult(byte[] Content, int PageCount);
    private sealed record ThemePalette(
        Color Primary,
        Color Muted,
        Color Border,
        Color SoftBackground,
        Color CoverBackground,
        Color CoverText,
        Color CoverMuted,
        Color Accent);
}
