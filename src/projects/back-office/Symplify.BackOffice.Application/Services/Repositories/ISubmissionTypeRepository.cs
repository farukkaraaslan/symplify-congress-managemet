using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ISubmissionTypeRepository : IAsyncRepository<SubmissionType, Guid>, IRepository<SubmissionType, Guid>
{
}
