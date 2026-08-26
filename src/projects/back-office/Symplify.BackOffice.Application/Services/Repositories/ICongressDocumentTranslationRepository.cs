using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Services.Repositories;

public interface ICongressDocumentTranslationRepository
    : IAsyncRepository<CongressDocumentTranslation, Guid>, IRepository<CongressDocumentTranslation, Guid>
{
}
