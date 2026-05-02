using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Constants;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Delete;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Delete;
public class DeletePaymentDocumentCommand : IRequest<DeletedPaymentDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetPaymentDocuments";
    public string[] Roles => new[] { PaymentDocumentsOperationClaims.Admin, PaymentDocumentsOperationClaims.Write, PaymentDocumentsOperationClaims.Delete };
    public class DeletePaymentDocumentCommandHandler : IRequestHandler<DeletePaymentDocumentCommand, DeletedPaymentDocumentResponse>
    {
        private readonly IPaymentDocumentRepository _repository; private readonly IMapper _mapper; private readonly PaymentDocumentBusinessRules _rules;
        public DeletePaymentDocumentCommandHandler(IPaymentDocumentRepository repository, IMapper mapper, PaymentDocumentBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedPaymentDocumentResponse> Handle(DeletePaymentDocumentCommand request, CancellationToken cancellationToken)
        {
            PaymentDocument? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.PaymentDocumentShouldExistWhenSelected(entity);
            PaymentDocument deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedPaymentDocumentResponse>(deletedEntity);
        }
    }
}
