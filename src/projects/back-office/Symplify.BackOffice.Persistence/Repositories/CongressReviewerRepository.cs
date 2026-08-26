using Core.Persistence.Repositories;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class CongressReviewerRepository : EfRepositoryBase<CongressReviewer, BackOfficeDbContext, Guid>, ICongressReviewerRepository
{
    public CongressReviewerRepository(BackOfficeDbContext context) : base(context)
    {
    }
}
