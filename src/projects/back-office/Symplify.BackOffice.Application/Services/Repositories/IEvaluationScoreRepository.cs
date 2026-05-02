using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IEvaluationScoreRepository : IAsyncRepository<EvaluationScore, Guid>, IRepository<EvaluationScore, Guid>
{
}
