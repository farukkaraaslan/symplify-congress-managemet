using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.ShortLinks;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class ShortLinkRepository : EfRepositoryBase<ShortLink, BackOfficeDbContext, Guid>, IShortLinkRepository
{
    public ShortLinkRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
