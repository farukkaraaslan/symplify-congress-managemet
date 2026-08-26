using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class ResourceKeyRepository : EfRepositoryBase<ResourceKey, BackOfficeDbContext, Guid>, IResourceKeyRepository
{
    public ResourceKeyRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
