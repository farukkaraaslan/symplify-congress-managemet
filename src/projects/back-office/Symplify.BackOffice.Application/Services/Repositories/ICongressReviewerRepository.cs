using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressReviewerRepository : IAsyncRepository<CongressReviewer, Guid>, IRepository<CongressReviewer, Guid>
{
}
