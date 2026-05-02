using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ISubmissionRepository : IAsyncRepository<Submission, Guid>, IRepository<Submission, Guid>
{
}
