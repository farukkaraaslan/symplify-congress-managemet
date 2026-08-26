using Symplify.BackOffice.Application.Features.AbstractBook.Models;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Services;

public interface IAbstractBookWordRenderer
{
    byte[] Render(AbstractBookDocumentModel model, string? culture);
}
