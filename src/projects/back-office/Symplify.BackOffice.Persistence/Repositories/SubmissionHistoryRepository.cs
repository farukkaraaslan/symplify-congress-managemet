using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class SubmissionHistoryRepository : EfRepositoryBase<SubmissionHistory, BackOfficeDbContext, Guid>, ISubmissionHistoryRepository
{
    public SubmissionHistoryRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
