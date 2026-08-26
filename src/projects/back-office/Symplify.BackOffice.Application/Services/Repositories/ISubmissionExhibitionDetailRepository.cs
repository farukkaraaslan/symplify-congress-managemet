using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ISubmissionExhibitionDetailRepository : IAsyncRepository<SubmissionExhibitionDetail, Guid>, IRepository<SubmissionExhibitionDetail, Guid>
{
}
