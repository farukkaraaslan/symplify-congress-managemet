using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Rules;
public class PaymentDocumentBusinessRules : BaseBusinessRules
{
    public Task PaymentDocumentShouldExistWhenSelected(PaymentDocument? entity)
    {
        if (entity is null) throw new BusinessException(PaymentDocumentsMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
