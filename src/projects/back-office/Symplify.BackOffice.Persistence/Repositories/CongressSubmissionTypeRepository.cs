using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class CongressSubmissionTypeRepository : EfRepositoryBase<CongressSubmissionType, BackOfficeDbContext, Guid>, ICongressSubmissionTypeRepository
{
    public CongressSubmissionTypeRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
