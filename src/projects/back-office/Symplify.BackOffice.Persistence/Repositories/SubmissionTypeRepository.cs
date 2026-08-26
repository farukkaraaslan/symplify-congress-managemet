using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class SubmissionTypeRepository : EfRepositoryBase<SubmissionType, BackOfficeDbContext, Guid>, ISubmissionTypeRepository
{
    public SubmissionTypeRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
