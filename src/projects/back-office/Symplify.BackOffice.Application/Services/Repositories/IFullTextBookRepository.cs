using Symplify.BackOffice.Application.Features.FullTextBook.Models;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IFullTextBookRepository
{
    Task<IReadOnlyList<FullTextBookFileSourceDto>> GetLatestApprovedFilesAsync(
        Guid congressId,
        CancellationToken cancellationToken);
}
