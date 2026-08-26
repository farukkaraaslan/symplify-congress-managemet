using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ISubmissionEvaluationRepository : IAsyncRepository<SubmissionEvaluation, Guid>, IRepository<SubmissionEvaluation, Guid>
{
}
