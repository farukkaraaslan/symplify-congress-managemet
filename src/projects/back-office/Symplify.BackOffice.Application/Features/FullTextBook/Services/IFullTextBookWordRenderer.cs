using Symplify.BackOffice.Application.Features.FullTextBook.Models;

namespace Symplify.BackOffice.Application.Features.FullTextBook.Services;

public interface IFullTextBookWordRenderer
{
    byte[] Render(FullTextBookDocumentModel model, string? culture);
}
