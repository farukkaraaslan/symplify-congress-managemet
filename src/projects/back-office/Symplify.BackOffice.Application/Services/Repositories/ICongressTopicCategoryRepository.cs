using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressTopicCategoryRepository
    : IAsyncRepository<CongressTopicCategory, Guid>, IRepository<CongressTopicCategory, Guid>
{
}
