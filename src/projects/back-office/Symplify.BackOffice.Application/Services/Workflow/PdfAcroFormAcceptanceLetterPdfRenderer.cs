using System.Text.RegularExpressions;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Layout.Element;
using iText.Layout.Properties;
using IOPath = System.IO.Path;

namespace Symplify.BackOffice.Application.Services.Workflow;

/// <summary>
/// Renders acceptance letters by filling a PDF AcroForm template.
///
/// Template source location:
/// Services/Workflow/Templates/AcceptanceLetters/acceptance-letter-acroform-template.pdf
///
/// Runtime copied location:
/// Templates/AcceptanceLetters/acceptance-letter-acroform-template.pdf
/// </summary>
public sealed class PdfAcroFormAcceptanceLetterPdfRenderer : IAcceptanceLetterPdfRenderer
{
    private const string DefaultTemplateFileName = "acceptance-letter-acroform-template.pdf";
    private const float BodyContentFontSize = 9.8f;
    private const float BodyContentHorizontalPadding = 4f;
    private const float BodyContentVerticalPadding = 3f;
    private const float BodyContentParagraphSpacing = 6f;
    private const float BodyContentLineHeight = 1.25f;
    private static readonly Regex OrdinalSuffixRegex = new(@"\b(?<number>\d+)(?<suffix>ST|ND|RD|TH)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex BodyParagraphSeparatorRegex = new(@"\n\s*\n", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public byte[] Render(AcceptanceLetterPdfModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        string templatePath = ResolveTemplatePath(model.Culture);

        using MemoryStream outputStream = new();
        using PdfReader reader = new(templatePath);
        using PdfWriter writer = new(outputStream);
        using PdfDocument pdfDocument = new(reader, writer);

        PdfAcroForm form = PdfAcroForm.GetAcroForm(pdfDocument, true);
        IDictionary<string, PdfFormField> fields = form.GetAllFormFields();

        ConfigureFieldAppearances(fields);
        FillTextFields(fields, model);
        StampJustifiedTextAndRemoveField(pdfDocument, form, fields, "BodyContent", model.BodyContent);

        // Logo placeholders are intentionally handled with both the current names and the
        // legacy alias. Some generated templates may contain a legacy blank widget named
        // OrganizationLogoImage on top of the left logo placeholder. If that orphan widget
        // is left in the form, FlattenFields can paint a blank appearance over the stamped
        // left logo. Therefore we stamp all supported aliases and then remove all logo
        // field aliases before flattening.
        StampImageAndRemoveField(pdfDocument, form, fields, "OrganizationLogoImageLeft", model.LogoBytes, 1.18f);
        StampImageAndRemoveField(pdfDocument, form, fields, "OrganizationLogoImage", model.LogoBytes, 1.18f);
        StampImageAndRemoveField(pdfDocument, form, fields, "OrganizationLogoImageRight", model.LogoBytes, 1.18f);
        RemoveFieldIfPresent(form, "OrganizationLogoImageLeft");
        RemoveFieldIfPresent(form, "OrganizationLogoImage");
        RemoveFieldIfPresent(form, "OrganizationLogoImageRight");

        StampImageAndRemoveField(pdfDocument, form, fields, "SignatureImage", model.SignatureBytes);
        StampImageAndRemoveField(pdfDocument, form, fields, "QrCodeImage", model.QrCodeBytes);

        form.FlattenFields();
        pdfDocument.Close();

        return outputStream.ToArray();
    }

    private static void ConfigureFieldAppearances(IDictionary<string, PdfFormField> fields)
    {
        PdfFontSet fontSet = CreatePdfFontSet();

        SetFieldAppearance(fields, "CongressTitle", fontSet.Bold, 16.8f);
        SetFieldAppearance(fields, "CongressLocation", fontSet.Bold, 8.8f);
        SetFieldAppearance(fields, "CongressDateRange", fontSet.Bold, 8.8f);
        SetFieldAppearance(fields, "BodyContent", fontSet.Regular, BodyContentFontSize);
        SetFieldAppearance(fields, "SignatoryName", fontSet.Bold, 8.8f);
        SetFieldAppearance(fields, "SignatoryDuty", fontSet.Regular, 7.7f);
        SetFieldAppearance(fields, "VerificationEmail", fontSet.Regular, 7.4f);
        SetFieldAppearance(fields, "VerificationCode", fontSet.Regular, 7.0f);
    }

    private static void SetFieldAppearance(
        IDictionary<string, PdfFormField> fields,
        string fieldName,
        PdfFont font,
        float fontSize)
    {
        if (!fields.TryGetValue(fieldName, out PdfFormField? field))
            return;

        field.SetFont(font);
        field.SetFontSize(fontSize);
    }

    private static PdfFontSet CreatePdfFontSet()
    {
        foreach ((string Regular, string Bold) candidate in BuildFontCandidates())
        {
            if (!File.Exists(candidate.Regular) || !File.Exists(candidate.Bold))
                continue;

            try
            {
                return new PdfFontSet(
                    PdfFontFactory.CreateFont(candidate.Regular, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED),
                    PdfFontFactory.CreateFont(candidate.Bold, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED));
            }
            catch
            {
                // Try the next font candidate. Rendering should not fail only because
                // one system font path is not usable in the current container/host.
            }
        }

        // Final fallback keeps rendering alive, but standard PDF fonts do not reliably
        // support Turkish glyphs. Production containers should provide DejaVu/Noto fonts.
        return new PdfFontSet(
            PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN),
            PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD));
    }

    private static IEnumerable<(string Regular, string Bold)> BuildFontCandidates()
    {
        string? configuredRegular = Environment.GetEnvironmentVariable("SYMPLIFY_ACCEPTANCE_PDF_FONT_REGULAR");
        string? configuredBold = Environment.GetEnvironmentVariable("SYMPLIFY_ACCEPTANCE_PDF_FONT_BOLD");

        if (!string.IsNullOrWhiteSpace(configuredRegular) && !string.IsNullOrWhiteSpace(configuredBold))
            yield return (configuredRegular.Trim(), configuredBold.Trim());

        string baseDirectory = AppContext.BaseDirectory;
        string currentDirectory = Directory.GetCurrentDirectory();

        yield return (
            IOPath.Combine(baseDirectory, "Templates", "Fonts", "DejaVuSerif.ttf"),
            IOPath.Combine(baseDirectory, "Templates", "Fonts", "DejaVuSerif-Bold.ttf"));

        yield return (
            IOPath.Combine(currentDirectory, "Templates", "Fonts", "DejaVuSerif.ttf"),
            IOPath.Combine(currentDirectory, "Templates", "Fonts", "DejaVuSerif-Bold.ttf"));

        yield return (
            "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf");

        yield return (
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf");

        yield return (
            "/usr/share/fonts/truetype/noto/NotoSerif-Regular.ttf",
            "/usr/share/fonts/truetype/noto/NotoSerif-Bold.ttf");

        yield return (
            "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
            "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf");

        yield return (
            @"C:\Windows\Fonts\times.ttf",
            @"C:\Windows\Fonts\timesbd.ttf");

        yield return (
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\arialbd.ttf");
    }

    private static void FillTextFields(IDictionary<string, PdfFormField> fields, AcceptanceLetterPdfModel model)
    {
        string headerCongressTitle = FormatCongressHeaderTitle(model.CongressTitle);
        string signatoryDuty = FirstNonEmpty(model.SignerDuty, "Organizing Committee");
        string organizationEmail = NormalizeFooterEmail(model.OrganizationEmail);
        string verificationEmail = organizationEmail;
        string verificationCodeText = string.IsNullOrWhiteSpace(model.VerificationCode)
            ? string.Empty
            : $"Verification Code: {model.VerificationCode.Trim()}";

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CongressTitle"] = headerCongressTitle,
            ["CongressLocation"] = model.CongressLocation,
            ["CongressDateRange"] = model.CongressDateRange,
            ["CongressTitleInline"] = model.CongressTitle,
            ["CongressDateRangeInline"] = model.CongressDateRange,
            ["SubmissionNumber"] = model.SubmissionCode,
            ["SubmissionCode"] = model.SubmissionCode,
            ["AuthorFullName"] = model.AuthorFullName,
            ["SubmissionTypeName"] = model.SubmissionTypeName,
            ["SubmissionTitle"] = model.SubmissionTitle,
            ["OrganizationShortName"] = model.OrganizationShortName,
            ["OrganizationName"] = model.OrganizationName,
            ["OrganizationEmail"] = organizationEmail,
            ["VerificationEmail"] = verificationEmail,
            ["SignatoryName"] = model.SignerFullName,
            ["SignatoryFullName"] = model.SignerFullName,
            ["SignerName"] = model.SignerFullName,
            ["SignatoryDuty"] = signatoryDuty,
            ["SignatoryTitle"] = signatoryDuty,
            ["SignerDuty"] = signatoryDuty,
            ["SignerTitleOrDuty"] = signatoryDuty,
            ["VerificationCode"] = verificationCodeText,
            ["VerificationUrl"] = model.VerificationUrl
        };

        foreach (KeyValuePair<string, string> item in values)
            SetFieldValue(fields, item.Key, item.Value);
    }

    private static void SetFieldValue(IDictionary<string, PdfFormField> fields, string fieldName, string? value)
    {
        if (!fields.TryGetValue(fieldName, out PdfFormField? field))
            return;

        field.SetValue(value ?? string.Empty);
    }

    private static void StampJustifiedTextAndRemoveField(
        PdfDocument pdfDocument,
        PdfAcroForm form,
        IDictionary<string, PdfFormField> fields,
        string fieldName,
        string? value)
    {
        if (!fields.TryGetValue(fieldName, out PdfFormField? field))
            return;

        PdfWidgetAnnotation? widget = field.GetWidgets().FirstOrDefault();
        if (widget is null)
        {
            RemoveFieldIfPresent(form, fieldName);
            return;
        }

        PdfPage? page = widget.GetPage();
        if (page is null)
        {
            RemoveFieldIfPresent(form, fieldName);
            return;
        }

        Rectangle rectangle = ApplyPadding(
            widget.GetRectangle().ToRectangle(),
            BodyContentHorizontalPadding,
            BodyContentVerticalPadding);

        if (rectangle.GetWidth() <= 0 || rectangle.GetHeight() <= 0)
        {
            RemoveFieldIfPresent(form, fieldName);
            return;
        }

        string[] paragraphs = SplitBodyParagraphs(value);
        if (paragraphs.Length == 0)
        {
            RemoveFieldIfPresent(form, fieldName);
            return;
        }

        PdfFontSet fontSet = CreatePdfFontSet();
        PdfCanvas pdfCanvas = new(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);
        iText.Layout.Canvas canvas = new(pdfCanvas, rectangle);

        try
        {
            foreach (string paragraphText in paragraphs)
            {
                Paragraph paragraph = new Paragraph(paragraphText)
                    .SetFont(fontSet.Regular)
                    .SetFontSize(BodyContentFontSize)
                    .SetTextAlignment(TextAlignment.JUSTIFIED)
                    .SetMultipliedLeading(BodyContentLineHeight)
                    .SetMargin(0)
                    .SetMarginBottom(BodyContentParagraphSpacing);

                canvas.Add(paragraph);
            }
        }
        finally
        {
            canvas.Close();
        }

        RemoveFieldIfPresent(form, fieldName);
    }

    private static string[] SplitBodyParagraphs(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        string normalized = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return BodyParagraphSeparatorRegex
            .Split(normalized)
            .Select(item => Regex.Replace(item, @"\s*\n\s*", " ").Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static Rectangle ApplyPadding(Rectangle source, float horizontalPadding, float verticalPadding)
    {
        float width = Math.Max(0, source.GetWidth() - (horizontalPadding * 2));
        float height = Math.Max(0, source.GetHeight() - (verticalPadding * 2));

        return new Rectangle(
            source.GetX() + horizontalPadding,
            source.GetY() + verticalPadding,
            width,
            height);
    }


    private static void RemoveFieldIfPresent(PdfAcroForm form, string fieldName)
    {
        try
        {
            form.RemoveField(fieldName);
        }
        catch
        {
            // Field removal is a best-effort cleanup for compatibility aliases.
            // Rendering should not fail only because a legacy placeholder is absent.
        }
    }

    private static void StampImageAndRemoveField(
        PdfDocument pdfDocument,
        PdfAcroForm form,
        IDictionary<string, PdfFormField> fields,
        string fieldName,
        byte[]? imageBytes,
        float targetScale = 1f)
    {
        if (!fields.TryGetValue(fieldName, out PdfFormField? field))
            return;

        if (imageBytes is not { Length: > 0 })
        {
            form.RemoveField(fieldName);
            return;
        }

        PdfWidgetAnnotation? widget = field.GetWidgets().FirstOrDefault();
        if (widget is null)
        {
            form.RemoveField(fieldName);
            return;
        }

        PdfPage? page = widget.GetPage();
        if (page is null)
        {
            form.RemoveField(fieldName);
            return;
        }

        Rectangle rectangle = ExpandRectangle(widget.GetRectangle().ToRectangle(), targetScale);
        Rectangle fittedRectangle = CalculateFittedImageRectangle(imageBytes, rectangle);
        ImageData imageData = ImageDataFactory.Create(imageBytes);

        PdfCanvas canvas = new(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);
        canvas.AddImageFittedIntoRectangle(imageData, fittedRectangle, false);

        form.RemoveField(fieldName);
    }

    private static Rectangle ExpandRectangle(Rectangle source, float scale)
    {
        if (scale <= 1f)
            return source;

        float width = source.GetWidth() * scale;
        float height = source.GetHeight() * scale;

        float x = source.GetX() - ((width - source.GetWidth()) / 2);
        float y = source.GetY() - ((height - source.GetHeight()) / 2);

        return new Rectangle(x, y, width, height);
    }

    private static Rectangle CalculateFittedImageRectangle(byte[] imageBytes, Rectangle target)
    {
        ImageData imageData = ImageDataFactory.Create(imageBytes);

        float imageWidth = imageData.GetWidth();
        float imageHeight = imageData.GetHeight();

        if (imageWidth <= 0 || imageHeight <= 0)
            return target;

        float scale = Math.Min(target.GetWidth() / imageWidth, target.GetHeight() / imageHeight);

        float width = imageWidth * scale;
        float height = imageHeight * scale;

        float x = target.GetX() + (target.GetWidth() - width) / 2;
        float y = target.GetY() + (target.GetHeight() - height) / 2;

        return new Rectangle(x, y, width, height);
    }

    private static string FormatCongressHeaderTitle(string? value)
    {
        string normalized = FirstNonEmpty(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        string upper = normalized.ToUpperInvariant();
        return OrdinalSuffixRegex.Replace(
            upper,
            match => $"{match.Groups["number"].Value}{match.Groups["suffix"].Value.ToLowerInvariant()}");
    }

    private static string NormalizeFooterEmail(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string ResolveTemplatePath(string? culture)
    {
        foreach (string candidate in BuildCandidatePaths(culture))
        {
            if (File.Exists(candidate))
                return candidate;
        }

        string searchedPaths = string.Join(Environment.NewLine, BuildCandidatePaths(culture));

        throw new FileNotFoundException(
            $"Acceptance letter PDF form template could not be found. Searched paths:{Environment.NewLine}{searchedPaths}");
    }

    private static IEnumerable<string> BuildCandidatePaths(string? culture)
    {
        string normalizedCulture = string.IsNullOrWhiteSpace(culture)
            ? "en"
            : culture.Trim();

        string[] templateRoots =
        {
            IOPath.Combine(AppContext.BaseDirectory, "Templates", "AcceptanceLetters"),
            IOPath.Combine(AppContext.BaseDirectory, "Services", "Workflow", "Templates", "AcceptanceLetters"),
            IOPath.Combine(Directory.GetCurrentDirectory(), "Templates", "AcceptanceLetters"),
            IOPath.Combine(Directory.GetCurrentDirectory(), "Services", "Workflow", "Templates", "AcceptanceLetters")
        };

        foreach (string templateRoot in templateRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return IOPath.Combine(
                templateRoot,
                $"acceptance-letter-acroform-template.{normalizedCulture}.pdf");

            int dashIndex = normalizedCulture.IndexOf('-', StringComparison.Ordinal);
            if (dashIndex > 0)
            {
                yield return IOPath.Combine(
                    templateRoot,
                    $"acceptance-letter-acroform-template.{normalizedCulture[..dashIndex]}.pdf");
            }

            yield return IOPath.Combine(templateRoot, DefaultTemplateFileName);
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
            ?? string.Empty;
    }

    private sealed record PdfFontSet(PdfFont Regular, PdfFont Bold);
}
