using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class SubmissionRepository : EfRepositoryBase<Submission, BackOfficeDbContext, Guid>, ISubmissionRepository
{
    public SubmissionRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
