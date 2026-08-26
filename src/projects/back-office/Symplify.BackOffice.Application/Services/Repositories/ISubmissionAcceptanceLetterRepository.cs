using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ISubmissionAcceptanceLetterRepository : IAsyncRepository<SubmissionAcceptanceLetter, Guid>, IRepository<SubmissionAcceptanceLetter, Guid>
{
}
