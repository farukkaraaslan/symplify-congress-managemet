using Symplify.BackOffice.Application.Features.AbstractBook.Models;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Services;

public interface IAbstractBookPdfRenderer
{
    byte[] Render(AbstractBookDocumentModel model, string? culture);
}
