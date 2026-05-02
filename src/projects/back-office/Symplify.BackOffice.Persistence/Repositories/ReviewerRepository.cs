using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public class ReviewerRepository : EfRepositoryBase<Reviewer, BackOfficeDbContext, Guid>, IReviewerRepository
{
    public ReviewerRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
