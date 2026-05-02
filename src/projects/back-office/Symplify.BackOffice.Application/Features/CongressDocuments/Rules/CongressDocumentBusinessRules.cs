using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
public class CongressDocumentBusinessRules : BaseBusinessRules
{
    public Task CongressDocumentShouldExistWhenSelected(CongressDocument? entity)
    {
        if (entity is null) throw new BusinessException(CongressDocumentsMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
