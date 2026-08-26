using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressTopicCategoryTranslationRepository
    : IAsyncRepository<CongressTopicCategoryTranslation, Guid>, IRepository<CongressTopicCategoryTranslation, Guid>
{
}
