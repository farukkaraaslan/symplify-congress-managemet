using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface IAuthorRepository : IAsyncRepository<Author, Guid>, IRepository<Author, Guid>
{
}
