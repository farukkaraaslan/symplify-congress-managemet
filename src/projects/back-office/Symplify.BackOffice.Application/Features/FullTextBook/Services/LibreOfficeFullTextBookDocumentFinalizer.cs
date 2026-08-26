using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Symplify.BackOffice.Application.Features.FullTextBook.Services;

/// <summary>
/// Expands altChunk parts and freezes calculated PAGEREF/PAGE values before the
/// generated full-text book is returned to the browser.
/// </summary>
public sealed class LibreOfficeFullTextBookDocumentFinalizer
    : IFullTextBookDocumentFinalizer
{
    private static readonly TimeSpan ConversionTimeout = TimeSpan.FromMinutes(5);

    private readonly ILogger<LibreOfficeFullTextBookDocumentFinalizer> _logger;

    public LibreOfficeFullTextBookDocumentFinalizer(
        ILogger<LibreOfficeFullTextBookDocumentFinalizer> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> FinalizeAsync(
        byte[] documentContent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentContent);

        if (documentContent.Length == 0)
            throw new InvalidOperationException("Sonlandırılacak tam metin kitabı boş.");

        string rootDirectory = Path.Combine(
            Path.GetTempPath(),
            "symplify-full-text-book",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(rootDirectory);

        try
        {
            // First pass expands imported DOCX altChunk parts and recalculates fields.
            byte[] firstPass = await ConvertPassAsync(
                documentContent,
                rootDirectory,
                "pass-1",
                cancellationToken);

            // Second pass stabilizes pagination after the imported parts have become
            // normal WordprocessingML content. This prevents later TOC page shifts.
            return await ConvertPassAsync(
                firstPass,
                rootDirectory,
                "pass-2",
                cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(rootDirectory);
        }
    }

    private async Task<byte[]> ConvertPassAsync(
        byte[] sourceContent,
        string rootDirectory,
        string passName,
        CancellationToken cancellationToken)
    {
        string passDirectory = Path.Combine(rootDirectory, passName);
        string inputDirectory = Path.Combine(passDirectory, "input");
        string outputDirectory = Path.Combine(passDirectory, "output");
        string profileDirectory = Path.Combine(passDirectory, "profile");

        Directory.CreateDirectory(inputDirectory);
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(profileDirectory);

        const string fileName = "full-text-book.docx";
        string inputPath = Path.Combine(inputDirectory, fileName);
        string outputPath = Path.Combine(outputDirectory, fileName);
        await File.WriteAllBytesAsync(inputPath, sourceContent, cancellationToken);

        string executable = ResolveLibreOfficeExecutable();
        ProcessStartInfo startInfo = new()
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = passDirectory
        };

        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--nodefault");
        startInfo.ArgumentList.Add("--nofirststartwizard");
        startInfo.ArgumentList.Add(
            $"-env:UserInstallation={new Uri(profileDirectory + Path.DirectorySeparatorChar).AbsoluteUri}");
        startInfo.ArgumentList.Add("--convert-to");
        startInfo.ArgumentList.Add("docx:Office Open XML Text");
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add(inputPath);

        startInfo.Environment["SAL_USE_VCLPLUGIN"] = "svp";

        using Process process = new() { StartInfo = startInfo };

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("LibreOffice işlemi başlatılamadı.");
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException(
                "Tam metin kitabının sayfa numaralarını kesinleştirmek için LibreOffice bulunamadı. " +
                "Windows ortamında LibreOffice kurun veya LIBREOFFICE_PATH değişkenini soffice.exe yoluna ayarlayın.",
                exception);
        }

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ConversionTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException(
                $"Tam metin kitabı sonlandırma işlemi {ConversionTimeout.TotalMinutes:0} dakika içinde tamamlanamadı.");
        }

        string standardOutput = await standardOutputTask;
        string standardError = await standardErrorTask;

        if (process.ExitCode != 0 || !File.Exists(outputPath))
        {
            _logger.LogError(
                "LibreOffice full-text book finalization failed. Pass: {PassName}, ExitCode: {ExitCode}, StdOut: {StdOut}, StdErr: {StdErr}",
                passName,
                process.ExitCode,
                standardOutput,
                standardError);

            throw new InvalidOperationException(
                "Tam metin kitabının sayfa numaraları ve içindekiler alanı kesinleştirilemedi.");
        }

        FileInfo outputFile = new(outputPath);
        if (outputFile.Length == 0)
            throw new InvalidOperationException("LibreOffice boş bir tam metin kitabı oluşturdu.");

        _logger.LogInformation(
            "Full-text book finalized successfully. Pass: {PassName}, OutputSize: {OutputSize}",
            passName,
            outputFile.Length);

        return await File.ReadAllBytesAsync(outputPath, cancellationToken);
    }

    private static string ResolveLibreOfficeExecutable()
    {
        string? configuredPath = Environment.GetEnvironmentVariable("LIBREOFFICE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return configuredPath.Trim();

        if (OperatingSystem.IsWindows())
        {
            string[] candidates =
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "LibreOffice", "program", "soffice.exe"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "LibreOffice", "program", "soffice.exe")
            };

            string? existing = candidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(existing))
                return existing;

            return "soffice.exe";
        }

        return File.Exists("/usr/bin/soffice")
            ? "/usr/bin/soffice"
            : "soffice";
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort process cleanup.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Temporary files are cleaned by the host/container lifecycle if deletion fails.
        }
    }
}
