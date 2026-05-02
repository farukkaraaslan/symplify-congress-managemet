using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressTopicRepository : IAsyncRepository<CongressTopic, Guid>, IRepository<CongressTopic, Guid>
{
}
