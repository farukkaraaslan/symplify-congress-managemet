using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressEvaluationCriterionRepository : IAsyncRepository<CongressEvaluationCriterion, Guid>, IRepository<CongressEvaluationCriterion, Guid>
{
}
