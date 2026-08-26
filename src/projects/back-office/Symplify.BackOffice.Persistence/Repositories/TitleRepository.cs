using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class TitleRepository : EfRepositoryBase<Title, BackOfficeDbContext, Guid>, ITitleRepository
{
    public TitleRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
