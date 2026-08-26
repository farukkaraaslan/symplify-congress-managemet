using Symplify.BackOffice.Application.Features.AbstractBook.Models;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IAbstractBookRepository
{
    Task<AbstractBookDocumentSourceDto?> GetDocumentSourceAsync(
        Guid congressId,
        IReadOnlyCollection<Guid> submissionIds,
        string? culture,
        CancellationToken cancellationToken);

    Task<string?> GetCongressLogoUrlAsync(
        Guid congressId,
        string? culture,
        CancellationToken cancellationToken);
}
