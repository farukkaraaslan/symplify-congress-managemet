using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressSectionRepository : EfRepositoryBase<CongressSection, BackOfficeDbContext, Guid>, ICongressSectionRepository
{
    public CongressSectionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
