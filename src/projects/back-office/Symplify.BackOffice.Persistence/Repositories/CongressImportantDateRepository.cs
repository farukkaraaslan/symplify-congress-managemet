using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressImportantDateRepository : EfRepositoryBase<CongressImportantDate, BackOfficeDbContext, Guid>, ICongressImportantDateRepository
{
    public CongressImportantDateRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
