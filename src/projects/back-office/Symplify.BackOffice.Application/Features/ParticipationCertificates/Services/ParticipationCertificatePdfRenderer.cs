using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

public sealed class ParticipationCertificatePdfRenderer : IParticipationCertificatePdfRenderer
{
    private static readonly string[] ParticipantNameTokens =
    {
        "{{PARTICIPANT_NAME}}",
        "{{AUTHOR_NAME}}",
        "{{AD_SOYAD}}",
        "[AD SOYAD]"
    };

    private static readonly string[] CertificateTextTokens =
    {
        "{{CERTIFICATE_TEXT}}",
        "{{PARTICIPATION_TEXT}}",
        "{{BELGE_METNI}}"
    };

    private static readonly string[] CommitteeSignatureTokens =
    {
        "{{COMMITTEE_SIGNATURE}}",
        "{{SIGNATURE}}",
        "{{DUZENLEME_KURULU_IMZA}}"
    };

    private static readonly string[] CommitteeSignerTokens =
    {
        "{{COMMITTEE_SIGNER}}",
        "{{COMMITTEE_NAME}}",
        "{{COMMITTEE_FULL_NAME}}",
        "{{DUZENLEME_KURULU_AD_SOYAD}}"
    };

    private static readonly string[] CommitteeRoleTokens =
    {
        "{{COMMITTEE_ROLE}}",
        "{{COMMITTEE_TITLE}}",
        "{{ORGANIZING_COMMITTEE_ROLE}}",
        "{{DUZENLEME_KURULU_UNVAN}}"
    };


    public void ValidateTemplate(byte[] templatePdfBytes)
    {
        if (templatePdfBytes is not { Length: > 0 })
            throw new InvalidOperationException("Katılım belgesi template PDF dosyası boş.");

        using MemoryStream inputStream = new(templatePdfBytes);
        using PdfReader reader = new(inputStream);
        using PdfDocument pdfDocument = new(reader);

        PdfPlaceholderLayout placeholders = PdfPlaceholderLayout.Extract(pdfDocument.GetFirstPage());
        List<string> missing = new();

        if (placeholders.FindFirst(ParticipantNameTokens) is null)
            missing.Add("{{PARTICIPANT_NAME}}");

        if (placeholders.FindFirst(CertificateTextTokens) is null)
            missing.Add("{{CERTIFICATE_TEXT}}");

        if (placeholders.FindFirst(CommitteeSignatureTokens) is null)
            missing.Add("{{COMMITTEE_SIGNATURE}}");

        if (placeholders.FindFirst(CommitteeSignerTokens) is null)
            missing.Add("{{COMMITTEE_SIGNER}}");

        if (placeholders.FindFirst(CommitteeRoleTokens) is null)
            missing.Add("{{COMMITTEE_ROLE}}");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Template PDF içinde zorunlu değişkenler eksik: {string.Join(", ", missing)}.");
        }
    }

    public byte[] Render(ParticipationCertificatePdfRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateTemplate(request.TemplatePdfBytes);

        using MemoryStream inputStream = new(request.TemplatePdfBytes);
        using MemoryStream outputStream = new();

        using PdfReader reader = new(inputStream);
        using PdfWriter writer = new(outputStream);
        using PdfDocument pdfDocument = new(reader, writer);

        PdfPage page = pdfDocument.GetFirstPage();
        Rectangle pageSize = page.GetPageSize();
        PdfPlaceholderLayout placeholders = PdfPlaceholderLayout.Extract(page);

        RenderParticipantName(pdfDocument, page, pageSize, placeholders, request);
        RenderCertificateText(pdfDocument, page, pageSize, placeholders, request);
        RenderCommitteeSignatureBlock(pdfDocument, page, pageSize, placeholders, request);

        pdfDocument.Close();
        return outputStream.ToArray();
    }

    private static void RenderParticipantName(
        PdfDocument pdfDocument,
        PdfPage page,
        Rectangle pageSize,
        PdfPlaceholderLayout placeholders,
        ParticipationCertificatePdfRenderRequest request)
    {
        PdfPlaceholderLocation? location = placeholders.FindFirst(ParticipantNameTokens);
        if (location is null)
        {
            throw new InvalidOperationException(
                "Katılım belgesi template PDF içinde katılımcı adı değişkeni bulunamadı. Template'e {{PARTICIPANT_NAME}} değişkenini ekleyin.");
        }

        Rectangle placeholderRectangle = Expand(location.Rectangle, 4f, pageSize);
        CoverPlaceholderIfEnabled(pdfDocument, page, placeholderRectangle, request);

        float targetWidth = Math.Min(pageSize.GetWidth() * 0.76f, Math.Max(location.Rectangle.GetWidth() * 2.6f, 430f));
        float targetHeight = Math.Max(location.Rectangle.GetHeight() * 2.8f, 46f);
        Rectangle nameRectangle = CenteredRectangle(location.Rectangle, pageSize, targetWidth, targetHeight);

        PdfFont font = ResolveFont();
        PdfCanvas pdfCanvas = CreateVisibleTextCanvas(pdfDocument, page);
        using Canvas canvas = new(pdfCanvas, nameRectangle);

        string participantName = NormalizeName(request.AuthorFullName);
        float requestedFontSize = request.NameFontSize > 0 ? request.NameFontSize : 30f;
        float fontSize = FitFontSize(
            font,
            participantName,
            requestedFontSize,
            minFontSize: 18f,
            availableWidth: nameRectangle.GetWidth() - 16f);

        Paragraph paragraph = new Paragraph(participantName)
            .SetFont(font)
            .SetFontSize(fontSize)
            .SetFontColor(ParseColor(request.NameFontColorHex, ColorConstants.WHITE))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetMultipliedLeading(1.0f)
            .SetMargin(0);

        canvas.ShowTextAligned(
            paragraph,
            nameRectangle.GetX() + nameRectangle.GetWidth() / 2,
            nameRectangle.GetY() + nameRectangle.GetHeight() / 2,
            TextAlignment.CENTER,
            VerticalAlignment.MIDDLE);
    }

    private static void RenderCertificateText(
        PdfDocument pdfDocument,
        PdfPage page,
        Rectangle pageSize,
        PdfPlaceholderLayout placeholders,
        ParticipationCertificatePdfRenderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CertificateText))
        {
            throw new InvalidOperationException(
                "Sertifika metni boş. Arayüzden sertifika metnini kaydetmeden belge oluşturamazsınız.");
        }

        PdfPlaceholderLocation? location = placeholders.FindFirst(CertificateTextTokens);
        if (location is null)
        {
            throw new InvalidOperationException(
                "Katılım belgesi template PDF içinde sertifika metni değişkeni bulunamadı. Template'e {{CERTIFICATE_TEXT}} değişkenini ekleyin.");
        }

        CoverPlaceholderIfEnabled(
            pdfDocument,
            page,
            Expand(location.Rectangle, 4f, pageSize),
            request);

        float targetWidth = Math.Min(
            pageSize.GetWidth() * 0.72f,
            Math.Max(location.Rectangle.GetWidth() * 4.5f, 460f));
        float targetHeight = Math.Min(
            pageSize.GetHeight() * 0.30f,
            Math.Max(location.Rectangle.GetHeight() * 7.0f, 105f));

        Rectangle textRectangle = CenteredRectangle(
            location.Rectangle,
            pageSize,
            targetWidth,
            targetHeight);

        PdfFont font = ResolveFont();
        float fontSize = ResolveCertificateTextFontSize(request.CertificateText);

        PdfCanvas pdfCanvas = CreateVisibleTextCanvas(pdfDocument, page);
        using Canvas canvas = new(pdfCanvas, textRectangle);

        Paragraph paragraph = new Paragraph(request.CertificateText.Trim())
            .SetFont(font)
            .SetFontSize(fontSize)
            .SetFontColor(ParseColor(request.NameFontColorHex, new DeviceRgb(20, 61, 151)))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMultipliedLeading(1.18f)
            .SetMargin(0);

        Div container = new Div()
            .SetWidth(textRectangle.GetWidth())
            .SetHeight(textRectangle.GetHeight())
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMargin(0)
            .SetPadding(0);

        container.Add(paragraph);
        canvas.Add(container);
    }

    private static void RenderCommitteeSignatureBlock(
        PdfDocument pdfDocument,
        PdfPage page,
        Rectangle pageSize,
        PdfPlaceholderLayout placeholders,
        ParticipationCertificatePdfRenderRequest request)
    {
        if (!request.RenderCommitteeSignature)
            return;

        PdfPlaceholderLocation? signatureLocation = placeholders.FindFirst(CommitteeSignatureTokens);
        PdfPlaceholderLocation? signerLocation = placeholders.FindFirst(CommitteeSignerTokens);
        PdfPlaceholderLocation? roleLocation = placeholders.FindFirst(CommitteeRoleTokens);

        if (signatureLocation is null && signerLocation is null && roleLocation is null)
        {
            throw new InvalidOperationException(
                "Katılım belgesi template PDF içinde düzenleme kurulu değişkenleri bulunamadı. Template'e {{COMMITTEE_SIGNATURE}} değişkenini ekleyin.");
        }

        Rectangle? signatureRectangle = null;
        if (signatureLocation is not null)
        {
            CoverPlaceholderIfEnabled(pdfDocument, page, Expand(signatureLocation.Rectangle, 4f, pageSize), request);
            signatureRectangle = CenteredRectangle(
                signatureLocation.Rectangle,
                pageSize,
                Math.Min(pageSize.GetWidth() * 0.24f, Math.Max(signatureLocation.Rectangle.GetWidth() * 1.8f, 145f)),
                Math.Max(signatureLocation.Rectangle.GetHeight() * 4.0f, 52f));

            RenderCommitteeSignatureImage(pdfDocument, page, pageSize, signatureRectangle, request);
        }

        string signerDisplayName = BuildSignerDisplayName(request.CommitteeSignerAcademicTitle, request.CommitteeSignerFullName);
        string role = FirstNonEmpty(request.CommitteeSignerRole, "Düzenleme Kurulu Başkanı");

        if (signerLocation is not null)
        {
            CoverPlaceholderIfEnabled(pdfDocument, page, Expand(signerLocation.Rectangle, 3f, pageSize), request);
            RenderCenteredText(
                pdfDocument,
                page,
                CenteredRectangle(signerLocation.Rectangle, pageSize, Math.Max(signerLocation.Rectangle.GetWidth() * 1.8f, 190f), Math.Max(signerLocation.Rectangle.GetHeight() * 1.8f, 22f)),
                signerDisplayName,
                10.5f,
                request.NameFontColorHex,
                bold: true);
        }
        else if (signatureRectangle is not null)
        {
            Rectangle autoNameRectangle = new(
                signatureRectangle.GetX() - 18f,
                Math.Max(0, signatureRectangle.GetY() - 18f),
                signatureRectangle.GetWidth() + 36f,
                18f);
            RenderCenteredText(pdfDocument, page, autoNameRectangle, signerDisplayName, 10f, request.NameFontColorHex, bold: true);
        }

        if (roleLocation is not null)
        {
            CoverPlaceholderIfEnabled(pdfDocument, page, Expand(roleLocation.Rectangle, 3f, pageSize), request);
            RenderCenteredText(
                pdfDocument,
                page,
                CenteredRectangle(roleLocation.Rectangle, pageSize, Math.Max(roleLocation.Rectangle.GetWidth() * 1.8f, 210f), Math.Max(roleLocation.Rectangle.GetHeight() * 1.8f, 20f)),
                role,
                9.5f,
                request.NameFontColorHex,
                bold: false);
        }
        else if (signatureRectangle is not null)
        {
            Rectangle autoRoleRectangle = new(
                signatureRectangle.GetX() - 18f,
                Math.Max(0, signatureRectangle.GetY() - 36f),
                signatureRectangle.GetWidth() + 36f,
                16f);
            RenderCenteredText(pdfDocument, page, autoRoleRectangle, role, 9f, request.NameFontColorHex, bold: false);
        }
    }

    private static void RenderCommitteeSignatureImage(
        PdfDocument pdfDocument,
        PdfPage page,
        Rectangle pageSize,
        Rectangle signatureRectangle,
        ParticipationCertificatePdfRenderRequest request)
    {
        if (request.CommitteeSignatureImageBytes is not { Length: > 0 })
            return;

        try
        {
            ImageData imageData = ImageDataFactory.Create(request.CommitteeSignatureImageBytes);
            Image image = new(imageData);
            image.ScaleToFit(signatureRectangle.GetWidth(), signatureRectangle.GetHeight());

            float imageWidth = image.GetImageScaledWidth();
            float imageHeight = image.GetImageScaledHeight();
            float imageX = signatureRectangle.GetX() + Math.Max(0, (signatureRectangle.GetWidth() - imageWidth) / 2f);
            float imageY = signatureRectangle.GetY() + Math.Max(0, (signatureRectangle.GetHeight() - imageHeight) / 2f);

            image.SetFixedPosition(1, imageX, imageY);

            PdfCanvas pdfCanvas = new(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);
            using Canvas canvas = new(pdfCanvas, pageSize);
            canvas.Add(image);
        }
        catch
        {
            // İmza görseli okunamazsa PDF'i bozma. Servis tarafı zaten imza yokluğunu üretim öncesi kontrol eder.
        }
    }

    private static void RenderCenteredText(
        PdfDocument pdfDocument,
        PdfPage page,
        Rectangle rectangle,
        string text,
        float fontSize,
        string colorHex,
        bool bold)
    {
        if (string.IsNullOrWhiteSpace(text) || rectangle.GetWidth() <= 0 || rectangle.GetHeight() <= 0)
            return;

        PdfFont font = ResolveFont(bold);
        PdfCanvas pdfCanvas = CreateVisibleTextCanvas(pdfDocument, page);
        using Canvas canvas = new(pdfCanvas, rectangle);

        Paragraph paragraph = new Paragraph(NormalizeName(text))
            .SetFont(font)
            .SetFontSize(fontSize)
            .SetFontColor(ParseColor(colorHex, ColorConstants.WHITE))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetMultipliedLeading(1.0f)
            .SetMargin(0);

        canvas.ShowTextAligned(
            paragraph,
            rectangle.GetX() + rectangle.GetWidth() / 2,
            rectangle.GetY() + rectangle.GetHeight() / 2,
            TextAlignment.CENTER,
            VerticalAlignment.MIDDLE);
    }

    private static PdfCanvas CreateVisibleTextCanvas(PdfDocument pdfDocument, PdfPage page)
    {
        PdfCanvas pdfCanvas = new(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);

        // Template placeholder'lari PDF icinde gorunmez metin olarak (3 Tr) tutulur.
        // PDF text rendering mode content stream'ler arasinda kalabildigi icin,
        // dinamik metin cizmeden once modu normal dolguya (0 Tr) geri aliyoruz.
        pdfCanvas
            .BeginText()
            .SetTextRenderingMode(PdfCanvasConstants.TextRenderingMode.FILL)
            .EndText();

        return pdfCanvas;
    }

    private static void CoverPlaceholderIfEnabled(
        PdfDocument pdfDocument,
        PdfPage page,
        Rectangle rectangle,
        ParticipationCertificatePdfRenderRequest request)
    {
        if (!request.CoverPlaceholderBackground)
            return;

        CoverPlaceholder(
            pdfDocument,
            page,
            rectangle,
            request.PlaceholderBackgroundColorHex);
    }

    private static void CoverPlaceholder(PdfDocument pdfDocument, PdfPage page, Rectangle rectangle, string backgroundColorHex)
    {
        if (rectangle.GetWidth() <= 0 || rectangle.GetHeight() <= 0)
            return;

        PdfCanvas canvas = new(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);
        canvas
            .SaveState()
            .SetFillColor(ParseColor(backgroundColorHex, new DeviceRgb(6, 20, 46)))
            .Rectangle(rectangle)
            .Fill()
            .RestoreState();
    }

    private static Rectangle CenteredRectangle(Rectangle source, Rectangle pageSize, float width, float height)
    {
        float centerX = source.GetX() + source.GetWidth() / 2f;
        float centerY = source.GetY() + source.GetHeight() / 2f;
        float x = Clamp(centerX - width / 2f, 0, Math.Max(0, pageSize.GetWidth() - width));
        float y = Clamp(centerY - height / 2f, 0, Math.Max(0, pageSize.GetHeight() - height));

        return new Rectangle(x, y, Math.Min(width, pageSize.GetWidth()), Math.Min(height, pageSize.GetHeight()));
    }

    private static Rectangle Expand(Rectangle source, float padding, Rectangle pageSize)
    {
        float x = Clamp(source.GetX() - padding, 0, pageSize.GetWidth());
        float y = Clamp(source.GetY() - padding, 0, pageSize.GetHeight());
        float right = Clamp(source.GetRight() + padding, 0, pageSize.GetWidth());
        float top = Clamp(source.GetTop() + padding, 0, pageSize.GetHeight());

        return new Rectangle(x, y, Math.Max(0, right - x), Math.Max(0, top - y));
    }

    private static PdfFont ResolveFont(bool bold = false)
    {
        foreach ((string Regular, string Bold) candidate in BuildFontCandidates())
        {
            string path = bold && File.Exists(candidate.Bold) ? candidate.Bold : candidate.Regular;
            if (!File.Exists(path))
                continue;

            try
            {
                return PdfFontFactory.CreateFont(
                    path,
                    iText.IO.Font.PdfEncodings.IDENTITY_H,
                    PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            }
            catch
            {
                // Try next candidate.
            }
        }

        return PdfFontFactory.CreateFont(bold ? StandardFonts.TIMES_BOLD : StandardFonts.TIMES_ROMAN);
    }

    private static IEnumerable<(string Regular, string Bold)> BuildFontCandidates()
    {
        string? configuredRegular = Environment.GetEnvironmentVariable("SYMPLIFY_CERTIFICATE_PDF_FONT_REGULAR");
        string? configuredBold = Environment.GetEnvironmentVariable("SYMPLIFY_CERTIFICATE_PDF_FONT_BOLD");

        if (!string.IsNullOrWhiteSpace(configuredRegular))
            yield return (configuredRegular.Trim(), configuredBold?.Trim() ?? configuredRegular.Trim());

        yield return (@"C:\Windows\Fonts\times.ttf", @"C:\Windows\Fonts\timesbd.ttf");
        yield return ("/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf");
        yield return ("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf");
        yield return ("/usr/share/fonts/truetype/liberation2/LiberationSerif-Regular.ttf", "/usr/share/fonts/truetype/liberation2/LiberationSerif-Bold.ttf");
    }

    private static string BuildSignerDisplayName(string? academicTitle, string? fullName)
    {
        string name = NormalizeName(fullName);
        string title = NormalizeName(academicTitle);

        return string.IsNullOrWhiteSpace(title) ? name : $"{title} {name}";
    }

    private static float ResolveCertificateTextFontSize(string text)
    {
        int length = text.Length;

        if (length <= 180)
            return 12f;

        if (length <= 260)
            return 11f;

        if (length <= 360)
            return 10f;

        return 9f;
    }

    private static float FitFontSize(
        PdfFont font,
        string text,
        float preferredFontSize,
        float minFontSize,
        float availableWidth)
    {
        float size = Math.Max(minFontSize, preferredFontSize);
        if (string.IsNullOrWhiteSpace(text) || availableWidth <= 0)
            return size;

        while (size > minFontSize && font.GetWidth(text, size) > availableWidth)
            size -= 0.5f;

        return Math.Max(minFontSize, size);
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        string hex = value.Trim().TrimStart('#');
        if (hex.Length != 6)
            return fallback;

        try
        {
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new DeviceRgb(r, g, b);
        }
        catch
        {
            return fallback;
        }
    }

    private static float Clamp(float value, float min, float max)
        => Math.Min(Math.Max(value, min), max);

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private sealed class PdfPlaceholderLayout : IEventListener
    {
        private readonly List<PdfTextChunk> _chunks = new();
        private readonly Dictionary<string, Rectangle> _locations = new(StringComparer.OrdinalIgnoreCase);

        public static PdfPlaceholderLayout Extract(PdfPage page)
        {
            PdfPlaceholderLayout listener = new();
            PdfCanvasProcessor processor = new(listener);
            processor.ProcessPageContent(page);
            listener.BuildLocations();
            return listener;
        }

        public PdfPlaceholderLocation? FindFirst(IEnumerable<string> tokens)
        {
            foreach (string token in tokens)
            {
                if (_locations.TryGetValue(token, out Rectangle? rectangle))
                    return new PdfPlaceholderLocation(token, rectangle);
            }

            return null;
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT || data is not TextRenderInfo textRenderInfo)
                return;

            string text = textRenderInfo.GetText();
            if (string.IsNullOrEmpty(text))
                return;

            Rectangle ascent = textRenderInfo.GetAscentLine().GetBoundingRectangle();
            Rectangle descent = textRenderInfo.GetDescentLine().GetBoundingRectangle();
            Rectangle rectangle = Union(ascent, descent);

            _chunks.Add(new PdfTextChunk(text, rectangle));
        }

        public ICollection<EventType> GetSupportedEvents()
            => new[] { EventType.RENDER_TEXT };

        private void BuildLocations()
        {
            string[] allTokens = ParticipantNameTokens
                .Concat(CertificateTextTokens)
                .Concat(CommitteeSignatureTokens)
                .Concat(CommitteeSignerTokens)
                .Concat(CommitteeRoleTokens)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (IGrouping<int, PdfTextChunk> line in _chunks.GroupBy(chunk => (int)Math.Round(chunk.Rectangle.GetY() / 2f)))
            {
                List<PdfTextChunk> ordered = line
                    .OrderBy(chunk => chunk.Rectangle.GetX())
                    .ToList();

                string lineText = string.Concat(ordered.Select(chunk => chunk.Text));
                if (lineText.Length == 0)
                    continue;

                List<(PdfTextChunk Chunk, int Start, int End)> spans = new();
                int cursor = 0;
                foreach (PdfTextChunk chunk in ordered)
                {
                    int start = cursor;
                    cursor += chunk.Text.Length;
                    spans.Add((chunk, start, cursor));
                }

                foreach (string token in allTokens)
                {
                    int index = lineText.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                    if (index < 0 || _locations.ContainsKey(token))
                        continue;

                    int end = index + token.Length;
                    List<PdfTextChunk> tokenChunks = spans
                        .Where(span => span.End > index && span.Start < end)
                        .Select(span => span.Chunk)
                        .ToList();

                    if (tokenChunks.Count == 0)
                        continue;

                    Rectangle rectangle = tokenChunks[0].Rectangle;
                    foreach (PdfTextChunk chunk in tokenChunks.Skip(1))
                        rectangle = Union(rectangle, chunk.Rectangle);

                    _locations[token] = rectangle;
                }
            }
        }

        private static Rectangle Union(Rectangle first, Rectangle second)
        {
            float x = Math.Min(first.GetX(), second.GetX());
            float y = Math.Min(first.GetY(), second.GetY());
            float right = Math.Max(first.GetRight(), second.GetRight());
            float top = Math.Max(first.GetTop(), second.GetTop());
            return new Rectangle(x, y, right - x, top - y);
        }
    }

    private sealed record PdfTextChunk(string Text, Rectangle Rectangle);

    private sealed record PdfPlaceholderLocation(string Token, Rectangle Rectangle);
}
