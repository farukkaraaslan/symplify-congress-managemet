using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IReviewerRepository : IAsyncRepository<Reviewer, Guid>, IRepository<Reviewer, Guid>
{
}
