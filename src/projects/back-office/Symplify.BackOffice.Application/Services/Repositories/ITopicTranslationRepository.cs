using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ITopicTranslationRepository : IAsyncRepository<TopicTranslation, Guid>, IRepository<TopicTranslation, Guid>
{
}
