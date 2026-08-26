using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.FullTextBook.Models;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class FullTextBookRepository : IFullTextBookRepository
{
    private readonly BackOfficeDbContext _context;

    public FullTextBookRepository(BackOfficeDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FullTextBookFileSourceDto>> GetLatestApprovedFilesAsync(
        Guid congressId,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            return Array.Empty<FullTextBookFileSourceDto>();

        var files = await _context.SubmissionFiles
            .AsNoTracking()
            .Where(file =>
                file.DeletedDate == null &&
                file.IsActive &&
                file.FileKind == SubmissionFileKind.FullText &&
                file.Submission.DeletedDate == null &&
                file.Submission.CongressId == congressId)
            .OrderBy(file => file.SubmissionId)
            .ThenByDescending(file => file.VersionNo)
            .ThenByDescending(file => file.CreatedDate)
            .ThenByDescending(file => file.Id)
            .Select(file => new
            {
                file.Id,
                file.SubmissionId,
                file.OriginalFileName,
                file.FilePath,
                file.ContentType,
                file.FileSize,
                file.VersionNo,
                file.ReviewStatus
            })
            .ToListAsync(cancellationToken);

        return files
            .GroupBy(file => file.SubmissionId)
            .Select(group => group.First())
            .Where(file => file.ReviewStatus == SubmissionFileReviewStatus.Approved)
            .Select(file => new FullTextBookFileSourceDto
            {
                FileId = file.Id,
                SubmissionId = file.SubmissionId,
                OriginalFileName = file.OriginalFileName,
                FilePath = file.FilePath,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                VersionNo = file.VersionNo
            })
            .ToList();
    }
}
