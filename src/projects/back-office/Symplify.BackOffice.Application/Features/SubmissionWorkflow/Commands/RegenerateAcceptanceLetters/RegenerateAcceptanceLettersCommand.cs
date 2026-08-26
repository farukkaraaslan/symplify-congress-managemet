using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.RegenerateAcceptanceLetters;

public sealed class RegenerateAcceptanceLettersCommand : IRequest<RegeneratedAcceptanceLettersResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionId { get; set; }

    public Guid? PerformedByUserId { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissions";

    public string[] Roles => new[]
    {
        SubmissionsOperationClaims.Admin,
        SubmissionsOperationClaims.Write,
        SubmissionsOperationClaims.Update
    };

    public sealed class Handler : IRequestHandler<RegenerateAcceptanceLettersCommand, RegeneratedAcceptanceLettersResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
        private readonly IAcceptanceLetterService _acceptanceLetterService;

        public Handler(
            ISubmissionRepository submissionRepository,
            ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
            IAcceptanceLetterService acceptanceLetterService)
        {
            _submissionRepository = submissionRepository;
            _acceptanceLetterRepository = acceptanceLetterRepository;
            _acceptanceLetterService = acceptanceLetterService;
        }

        public async Task<RegeneratedAcceptanceLettersResponse> Handle(
            RegenerateAcceptanceLettersCommand request,
            CancellationToken cancellationToken)
        {
            if (request.SubmissionId == Guid.Empty)
                return RegeneratedAcceptanceLettersResponse.Failed(request.SubmissionId, "Bildiri bilgisi geçersiz.");

            Submission? submission = await _submissionRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.TransactionStatus)
                .Include(item => item.Authors)
                .FirstOrDefaultAsync(item => item.Id == request.SubmissionId, cancellationToken);

            if (submission is null)
                return RegeneratedAcceptanceLettersResponse.Failed(request.SubmissionId, "Bildiri bulunamadı.");

            if (!IsCode(submission.TransactionStatus?.Code, "ACCEPTED"))
                return RegeneratedAcceptanceLettersResponse.Failed(request.SubmissionId, "Kabul belgesi sadece kabul edilmiş bildiriler için yenilenebilir.");

            List<Guid> activeAuthorIds = submission.Authors
                .Where(author => author.DeletedDate == null)
                .Select(author => author.Id)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (activeAuthorIds.Count == 0)
                return RegeneratedAcceptanceLettersResponse.Failed(request.SubmissionId, "Bildiriye bağlı aktif yazar bulunamadı.");

            IReadOnlyList<SubmissionAcceptanceLetter> letters = await _acceptanceLetterService.ReplaceCurrentAsync(submission, request.PerformedByUserId, cancellationToken);

            int updatedCount = await CountCurrentLanguageLettersAsync(
                submission.Id,
                submission.LanguageId,
                activeAuthorIds,
                cancellationToken);

            int regeneratedCount = letters.Count;
            int missingCount = Math.Max(0, activeAuthorIds.Count - updatedCount);

            string message = regeneratedCount > 0
                ? $"Kabul belgeleri güncellendi. Güncel belge sayısı: {regeneratedCount}."
                : "Kabul belgesi yenilenemedi.";

            if (missingCount > 0)
                message += $" Hâlâ eksik belge sayısı: {missingCount}.";

            return RegeneratedAcceptanceLettersResponse.Ok(
                request.SubmissionId,
                activeAuthorIds.Count,
                updatedCount,
                regeneratedCount,
                missingCount,
                message);
        }

        private async Task<int> CountCurrentLanguageLettersAsync(
            Guid submissionId,
            Guid? languageId,
            IReadOnlyCollection<Guid> activeAuthorIds,
            CancellationToken cancellationToken)
        {
            if (activeAuthorIds.Count == 0)
                return 0;

            return await _acceptanceLetterRepository
                .Query()
                .AsNoTracking()
                .Where(letter =>
                    letter.SubmissionId == submissionId &&
                    letter.DeletedDate == null &&
                    letter.AuthorId.HasValue &&
                    activeAuthorIds.Contains(letter.AuthorId.Value) &&
                    letter.LanguageId == languageId)
                .Select(letter => letter.AuthorId!.Value)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        private static bool IsCode(string? value, params string[] expectedCodes)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized = NormalizeCode(value);
            return expectedCodes.Any(expected => NormalizeCode(expected) == normalized);
        }

        private static string NormalizeCode(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : new string(value.Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }
}

public sealed class RegeneratedAcceptanceLettersResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid SubmissionId { get; init; }
    public int AuthorCount { get; init; }
    public int LetterCount { get; init; }
    public int CreatedCount { get; init; }
    public int MissingCount { get; init; }

    public static RegeneratedAcceptanceLettersResponse Ok(
        Guid submissionId,
        int authorCount,
        int letterCount,
        int createdCount,
        int missingCount,
        string message)
        => new()
        {
            Success = true,
            SubmissionId = submissionId,
            AuthorCount = authorCount,
            LetterCount = letterCount,
            CreatedCount = createdCount,
            MissingCount = missingCount,
            Message = message
        };

    public static RegeneratedAcceptanceLettersResponse Failed(Guid submissionId, string message)
        => new()
        {
            Success = false,
            SubmissionId = submissionId,
            Message = message
        };
}
