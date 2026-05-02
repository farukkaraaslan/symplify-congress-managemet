using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Constants;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Queries.GetById;
public class GetByIdPaymentDocumentQuery : IRequest<GetByIdPaymentDocumentResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { PaymentDocumentsOperationClaims.Admin, PaymentDocumentsOperationClaims.Read };
    public class GetByIdPaymentDocumentQueryHandler : IRequestHandler<GetByIdPaymentDocumentQuery, GetByIdPaymentDocumentResponse>
    {
        private readonly IPaymentDocumentRepository _repository; private readonly IMapper _mapper; private readonly PaymentDocumentBusinessRules _rules;
        public GetByIdPaymentDocumentQueryHandler(IPaymentDocumentRepository repository, IMapper mapper, PaymentDocumentBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdPaymentDocumentResponse> Handle(GetByIdPaymentDocumentQuery request, CancellationToken cancellationToken)
        {
            PaymentDocument? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.PaymentDocumentShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdPaymentDocumentResponse>(entity);
        }
    }
}
