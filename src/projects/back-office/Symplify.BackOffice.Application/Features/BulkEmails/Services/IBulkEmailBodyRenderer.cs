using Symplify.BackOffice.Application.Features.BulkEmails.Dtos;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Services;

public interface IBulkEmailBodyRenderer
{
    BulkEmailBodyRenderResult Render(string plainText);
}
