using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IEvaluationCriterionTranslationRepository : IAsyncRepository<EvaluationCriterionTranslation, Guid>, IRepository<EvaluationCriterionTranslation, Guid>
{
}
