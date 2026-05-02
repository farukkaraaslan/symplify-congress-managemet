using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Services.Repositories;
public interface ILanguageRepository : IAsyncRepository<Language, Guid>, IRepository<Language, Guid>
{
}
