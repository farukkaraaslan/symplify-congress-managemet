using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ISubmissionTypeTranslationRepository : IAsyncRepository<SubmissionTypeTranslation, Guid>, IRepository<SubmissionTypeTranslation, Guid>
{
}
