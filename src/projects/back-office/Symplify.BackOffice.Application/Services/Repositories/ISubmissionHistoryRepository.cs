using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ISubmissionHistoryRepository : IAsyncRepository<SubmissionHistory, Guid>, IRepository<SubmissionHistory, Guid>
{
}
