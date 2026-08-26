using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class SubmissionExhibitionDetailRepository : EfRepositoryBase<SubmissionExhibitionDetail, BackOfficeDbContext, Guid>, ISubmissionExhibitionDetailRepository
{
    public SubmissionExhibitionDetailRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
