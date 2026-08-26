using Symplify.BackOffice.Application.Features.FullTextBook.Models;

namespace Symplify.BackOffice.Application.Features.FullTextBook.Services;

public interface IFullTextBookDocumentBuilder
{
    Task<FullTextBookDocumentModel> BuildAsync(
        FullTextBookBuildRequest request,
        CancellationToken cancellationToken);
}
