using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressSectionRepository : IAsyncRepository<CongressSection, Guid>, IRepository<CongressSection, Guid>
{
}
