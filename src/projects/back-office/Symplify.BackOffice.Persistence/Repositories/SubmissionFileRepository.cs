using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class SubmissionFileRepository : EfRepositoryBase<SubmissionFile, BackOfficeDbContext, Guid>, ISubmissionFileRepository
{
    public SubmissionFileRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
