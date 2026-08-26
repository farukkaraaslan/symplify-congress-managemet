using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ISubmissionFileRepository : IAsyncRepository<SubmissionFile, Guid>, IRepository<SubmissionFile, Guid>
{
}
