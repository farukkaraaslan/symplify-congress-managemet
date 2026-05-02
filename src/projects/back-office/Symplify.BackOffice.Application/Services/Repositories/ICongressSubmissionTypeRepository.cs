using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressSubmissionTypeRepository : IAsyncRepository<CongressSubmissionType, Guid>, IRepository<CongressSubmissionType, Guid>
{
}
