using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.WebUI.Controllers;

[AllowAnonymous]
[Route("verify/acceptance-letter")]
public sealed class AcceptanceLetterVerificationController : Controller
{
    private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;

    public AcceptanceLetterVerificationController(ISubmissionAcceptanceLetterRepository acceptanceLetterRepository)
    {
        _acceptanceLetterRepository = acceptanceLetterRepository;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Verify(string code, CancellationToken cancellationToken)
    {
        string normalizedCode = string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : code.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedCode))
            return Content(BuildInvalidHtml(), "text/html");

        SubmissionAcceptanceLetter? letter = await _acceptanceLetterRepository
            .Query()
            .AsNoTracking()
            .Include(item => item.Submission)
                .ThenInclude(submission => submission.Congress)
                    .ThenInclude(congress => congress.Organization)
            .FirstOrDefaultAsync(
                item => item.LetterNumber == normalizedCode && item.DeletedDate == null,
                cancellationToken);

        return Content(letter is null ? BuildInvalidHtml() : BuildValidHtml(letter), "text/html");
    }

    private static string BuildValidHtml(SubmissionAcceptanceLetter letter)
    {
        string organization = WebUtility.HtmlEncode(letter.Submission.Congress.Organization?.ShortName ?? "-");
        string congress = WebUtility.HtmlEncode(letter.Submission.Congress.Name);
        string submissionNumber = WebUtility.HtmlEncode(letter.Submission.SubmissionNumber);
        string author = WebUtility.HtmlEncode(letter.AuthorFullNameSnapshot);
        string generatedAt = letter.GeneratedAt.ToString("dd MMMM yyyy HH:mm");
        string code = WebUtility.HtmlEncode(letter.LetterNumber);

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>Acceptance Letter Verification</title>
                <style>
                    body { font-family: Arial, sans-serif; background:#f6f8fc; color:#0b1d3a; margin:0; padding:40px; }
                    .card { max-width:720px; margin:auto; background:#fff; border:1px solid #d1d5db; border-radius:16px; padding:28px; }
                    .badge { display:inline-block; background:#dcfce7; color:#166534; border-radius:999px; padding:8px 14px; font-weight:700; }
                    dl { display:grid; grid-template-columns:180px 1fr; gap:10px 16px; }
                    dt { color:#4b5563; }
                    dd { margin:0; font-weight:600; }
                </style>
            </head>
            <body>
                <main class="card">
                    <span class="badge">Verified</span>
                    <h1>Acceptance letter is valid</h1>
                    <dl>
                        <dt>Verification Code</dt><dd>{{code}}</dd>
                        <dt>Organization</dt><dd>{{organization}}</dd>
                        <dt>Congress</dt><dd>{{congress}}</dd>
                        <dt>Submission Number</dt><dd>{{submissionNumber}}</dd>
                        <dt>Author</dt><dd>{{author}}</dd>
                        <dt>Generated At</dt><dd>{{generatedAt}}</dd>
                    </dl>
                </main>
            </body>
            </html>
            """;
    }

    private static string BuildInvalidHtml()
    {
        return """
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>Acceptance Letter Verification</title>
                <style>
                    body { font-family: Arial, sans-serif; background:#f6f8fc; color:#0b1d3a; margin:0; padding:40px; }
                    .card { max-width:720px; margin:auto; background:#fff; border:1px solid #fecaca; border-radius:16px; padding:28px; }
                    .badge { display:inline-block; background:#fee2e2; color:#991b1b; border-radius:999px; padding:8px 14px; font-weight:700; }
                </style>
            </head>
            <body>
                <main class="card">
                    <span class="badge">Not verified</span>
                    <h1>Acceptance letter could not be verified</h1>
                    <p>The verification code is invalid or the document is no longer active.</p>
                </main>
            </body>
            </html>
            """;
    }
}
