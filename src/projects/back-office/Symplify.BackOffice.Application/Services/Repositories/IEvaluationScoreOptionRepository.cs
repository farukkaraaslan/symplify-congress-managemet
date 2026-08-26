using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface IEvaluationScoreOptionRepository : IAsyncRepository<EvaluationScoreOption, Guid>, IRepository<EvaluationScoreOption, Guid>
{
}
