using Symplify.BackOffice.Application.Features.AbstractBook.Models;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Services;

public interface IAbstractBookDocumentBuilder
{
    Task<AbstractBookDocumentModel> BuildAsync(
        AbstractBookBuildRequest request,
        CancellationToken cancellationToken);
}
