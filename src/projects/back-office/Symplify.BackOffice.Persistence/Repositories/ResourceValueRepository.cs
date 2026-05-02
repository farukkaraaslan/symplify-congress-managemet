using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class ResourceValueRepository : EfRepositoryBase<ResourceValue, BackOfficeDbContext, Guid>, IResourceValueRepository
{
    public ResourceValueRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
