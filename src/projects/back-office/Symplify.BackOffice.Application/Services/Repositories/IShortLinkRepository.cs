using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.ShortLinks;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IShortLinkRepository : IAsyncRepository<ShortLink, Guid>, IRepository<ShortLink, Guid>
{
}
