using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ICongressBoardMemberRepository : IAsyncRepository<CongressBoardMember, Guid>, IRepository<CongressBoardMember, Guid>
{
}
